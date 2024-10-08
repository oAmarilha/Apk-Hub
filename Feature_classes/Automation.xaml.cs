using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using MahApps.Metro.Controls;
using Python.Runtime;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Concurrent;
using System.Windows.Media;
using System.Windows.Controls.Primitives;
using System.Text;
using System.Windows.Threading;
using System.Diagnostics;

namespace ApkInstaller
{
    public partial class Automation : Window, IComponentConnector
    {
        MainWindow mainWindow = MainWindow.Instance;
        private dynamic executorInstance;
        private dynamic sys;
        private DispatcherTimer outputTimer;
        private StringBuilder outputBuffer = new StringBuilder();
        private bool isRunning = false;

        // Fila de tarefas para garantir que tudo seja processado na mesma thread do Python
        private readonly BlockingCollection<Action> pythonTaskQueue = new BlockingCollection<Action>();

        private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        // Thread dedicada ao Python
        private Thread pythonThread;

        private Dictionary<string, (string, string, string)> appStringMap = new Dictionary<string, (string, string, string)>
        {
            { "Bobby's Canvas", ("com.sec.kidsplat.drawing", "Bobby's Canvas", "KidsCanvas") },
            { "Crocro's Friends Village", ("com.sec.android.app.kids3d", "Crocro's Friend Village", "KidsHouse") },
            { "Lisa's Music Band", ("com.sec.kidsplat.media.kidsmusic", "Lisa's Music Band", "KidsMusicBand") },
            { "My Magic Voice", ("com.sec.kidsplat.kidstalk", "My Magic Voice", "KidsMagicVoice") },
            { "Crocro's Adventure", ("com.sec.kidsplat.kidsbcg", "Crocro's Adventure", "KidsAdventure") },
            { "My Browser", ("com.sec.kidsplat.kidsbrowser", "My Browser", "KidsBrowser") },
            { "My Phone", ("com.sec.kidsplat.phone", "My Phone", "KidsPhone") },
            { "My Camera", ("com.sec.kidsplat.camera", "My Camera", "KidsCamera") },
            { "My Gallery", ("com.sec.kidsplat.kidsgallery", "My Gallery", "KidsGallery") },
            { "My Art Studio", ("br.org.sidi.kidsplat.artstudio", "My Art Studio", "KidsStudio") }
        };

        public Automation()
        {
            InitializeComponent();
            this.Owner = mainWindow;
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            string localapp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\Programs\\Python\\";
            if (Directory.Exists($"{localapp}Python312\\"))
            {
                Runtime.PythonDLL = $"{localapp}Python312\\python312.dll";
            }
            else if (Directory.Exists($"{localapp}Python311\\"))
            {
                Runtime.PythonDLL = $"{localapp}Python311\\python311.dll";
            }
            else if (Directory.Exists($"{localapp}Python310\\"))
            {
                Runtime.PythonDLL = $"{localapp}Python310\\python310.dll";
            }

            //Runtime.PythonDLL = @$"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\Programs\Python\Python312\python312.dll";

            // Inicializar a thread Python
            InitializePythonThread();

        }

        // Função para habilitar/desabilitar todos os botões
        private void ToogleElements(Panel panel, bool isEnabled)
        {
            foreach (var control in panel.Children)
            {
                if (control is StackPanel stackpanel)
                {
                    stackpanel.IsEnabled = isEnabled; // Altera o estado do painel
                }else if (control is UniformGrid uniformgrid)
                {
                    uniformgrid.IsEnabled = isEnabled;
                }
            }
        }

        // Inicializa a thread dedicada ao Python
        private void InitializePythonThread()
        {
            cancellationTokenSource = new CancellationTokenSource(); // Inicializa o token de cancelamento

            ToogleElements(AutomationGrid, false);
            pythonThread = new Thread(() =>
            {
                PythonEngine.Initialize();
                using (Py.GIL())
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        StatusText.Text = "Initializing Python AirTest, please wait...";
                    });

                    sys = Py.Import("sys");
                    sys.path.append(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Python\\scripts"));

                    dynamic pythonScript = Py.Import("executor");
                    executorInstance = pythonScript.Executor();
                    executorInstance.new_request();

                    // Obtém informações do executor
                    string model = executorInstance.model.ToString();
                    string androidVersion = executorInstance.osVersion.ToString();
                    string buildMode = executorInstance.build.ToString();
                    string themeMode = executorInstance.themeMode.ToString();
                    PyTuple res = executorInstance.res;
                    string resolution = $"{res[0].ToString()} x {res[1].ToString()}";

                    // Atualiza a UI com o modelo do dispositivo
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        DeviceModelText.Text = model;
                        AndroidVersionText.Text = androidVersion;
                        BuildModeText.Text = buildMode;
                        UiModeText.Text = themeMode;
                        ResolutionText.Text = resolution;
                        StatusText.Text = "Python Airtest correctly initialized";
                        StatusText.Foreground = Brushes.Green;
                        ToogleElements(AutomationGrid, true);
                    });
                }

                // Fila de tarefas: processa as tarefas enviadas à fila
                while (!pythonTaskQueue.IsCompleted)
                {
                    try
                    {
                        if (pythonTaskQueue.TryTake(out var task, Timeout.Infinite, cancellationTokenSource.Token))
                        {
                            task(); // Executa a tarefa Python
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        // Operação cancelada, saia do loop
                        break;
                    }
                }
                PythonEngine.Shutdown();
                bool test2 = PythonEngine.IsInitialized;
                if (this.IsVisible) Application.Current.Dispatcher.Invoke(() => StatusText.Text = "Python stopped");
            });
            pythonThread.Start();
        }

        private void UpdateOutput()
        {
            // Lê a saída do log e atualiza a TextBox
            string log_output = executorInstance.log_stream.getvalue(); // Lê o valor do StringIO
            if (!string.IsNullOrEmpty(log_output))
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    StatusText.Text += log_output; // Atualiza a TextBox
                    StatusText.ScrollToEnd();
                });

                // Limpa o log_stream para evitar repetição
                executorInstance.log_stream.truncate(0);
                executorInstance.log_stream.seek(0);
            }
        }

        // Função para executar código Python na thread Python
        private Task ExecutePythonActionAsync(Action pythonAction)
        {
            var tcs = new TaskCompletionSource<bool>();

            // Envia a ação para a fila de tarefas Python
            pythonTaskQueue.Add(() =>
            {
                try
                {
                    pythonAction(); // Executa a ação Python
                    tcs.SetResult(true); // Conclui a tarefa
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex); // Define exceção se houver erro
                }
            });

            return tcs.Task;
        }

        // Função que roda o script Python de maneira assíncrona
        private async Task RunPythonScriptAsync(List<string> appInstance)
        {
            var report = string.Empty;
            bool isError = false;
            Application.Current.Dispatcher.Invoke(() =>
            {
                //ToogleElements(AutomationGrid, false);
                StatusText.Text = string.Empty;
                StatusText.Foreground = Brushes.White;
            });

            // Inicia o timer para capturar a saída em tempo real
            outputTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            outputTimer.Tick += (s, e) => UpdateOutput();
            outputTimer.Start();

            await ExecutePythonActionAsync(() =>
            {
                using (Py.GIL())
                {
                    if (cancellationTokenSource.Token.IsCancellationRequested) return; // Verifica se o cancelamento foi solicitado

                    executorInstance.app_instance = appInstance;
                    try
                    {
                        var result = executorInstance.initialSetup();
                        report = executorInstance.logname.ToString();
                    }
                    catch
                    {
                        isError = true;
                    }
                }
            });

            outputTimer.Stop();
            isRunning = false;
            Start_Stop.Content = "Start";
            Start_Stop.Background = Brushes.Green;
            Application.Current.Dispatcher.Invoke(() =>
            {
                //ToogleElements(AutomationGrid, true);
            });

            if (!isError && !report.Contains("None") && this.IsVisible)
            {
                MessageBoxResult result = MessageBox.Show("Do you want to check the results", "Automation Finished", MessageBoxButton.YesNo, MessageBoxImage.Information);
                if (result == MessageBoxResult.Yes)
                {
                    Process.Start(new ProcessStartInfo()
                    {
                        FileName = $"{report}\\log.html",
                        UseShellExecute = true,
                        Verb = "open"
                    });
                }
            }
            else if (this.IsVisible)
            {
                MessageBox.Show("For some reason the automation was not executed, check the log for more info",
                    "Error found",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            ToogleElements(AutomationGrid, true);
        }

        // Função para parar a execução da thread Python
        public async Task StopPythonExecution()
        {
            if (isRunning)
            {
                outputTimer.Stop();
                using (Py.GIL())
                {
                    executorInstance.cancellation_request();
                }
            }
            await Task.Run(() =>
            {
                pythonTaskQueue.CompleteAdding();
                cancellationTokenSource.Cancel();
                Application.Current.Dispatcher.Invoke(() =>
                {
                    ToogleElements(AutomationGrid, false);
                    StatusText.Text = "Waiting python to be stopped...";
                    StatusText.Foreground = Brushes.Red;
                });
            });
            isRunning = false;
        }

        private async void Start_Stop_Click(object sender, RoutedEventArgs e)
        {
            Button? button = sender as Button;
            if (isRunning)
            {
                using (Py.GIL())
                {
                    executorInstance.cancellation_request();
                }
                // Se a execução estiver em andamento, interrompa-a e altere o conteúdo do botão para "Start"
                await StopPythonExecution();
                button!.Content = "Start";
                button!.Background = Brushes.Green;
            }
            else
            {
                if (cancellationTokenSource.IsCancellationRequested)
                {
                    InitializePythonThread();
                }
                List<string> selectedAppValues = new List<string>();
                List<string> selectedAppNames = new List<string>();
                List<string> selectedAppInstance = new List<string>();

                foreach (object child in AppsStackPanel.Children)
                {
                    if (child is CheckBox { IsChecked: true } checkBox)
                    {
                        string checkBoxContent = checkBox.Content.ToString();
                        if (appStringMap.TryGetValue(checkBoxContent, out (string, string, string) value))
                        {
                            selectedAppValues.Add(value.Item1);
                            selectedAppNames.Add(value.Item2);
                            selectedAppInstance.Add(value.Item3);
                        }
                    }
                }

                button!.Content = "Stop";  // Altera o conteúdo do botão para "Stop"
                button!.Background = Brushes.Red;
                isRunning = true;

                await RunPythonScriptAsync(selectedAppInstance); // Executa o script Python de forma assíncrona
            }
        }
    }
}
