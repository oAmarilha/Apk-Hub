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

namespace ApkInstaller
{
    public partial class Automation : Window, IComponentConnector
    {
        MainWindow mainWindow = MainWindow.Instance;
        private dynamic executorInstance;
        private dynamic sys;

        // Fila de tarefas para garantir que tudo seja processado na mesma thread do Python
        private readonly BlockingCollection<Action> pythonTaskQueue = new BlockingCollection<Action>();

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

            Runtime.PythonDLL = @"C:\Users\v.amarilha\appdata\local\Programs\Python\Python312\python312.dll";

            // Inicializar a thread Python
            InitializePythonThread();

            this.Closing += Automation_Closing;
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
                    string model = executorInstance.model.ToString();
                    string androidVersion = executorInstance.osVersion.ToString();
                    string buildMode = executorInstance.build.ToString();
                    string themeMode = executorInstance.themeMode.ToString();
                    PyTuple res = executorInstance.res;
                    string resolution = $"{res[0].ToString()} x {res[1].ToString()}";
                    // Atualizar a UI com o modelo do dispositivo
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        DeviceModelText.Text = model;
                        AndroidVersionText.Text = androidVersion;
                        BuildModeText.Text = buildMode;
                        UiModeText.Text= themeMode;
                        ResolutionText.Text = resolution;
                        StatusText.Text = "Initializing Finished";
                        StatusText.Foreground = Brushes.Green;
                        ToogleElements(AutomationGrid, true);
                    });
                }

                // Fila de tarefas: processa as tarefas enviadas à fila
                while (!pythonTaskQueue.IsCompleted)
                {
                    if (pythonTaskQueue.TryTake(out var task))
                    {
                        task(); // Executa a tarefa Python
                    }
                }
                PythonEngine.Shutdown();
            });

            pythonThread.Start();
        }

        private void Automation_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            // Fecha a fila e aguarda o término da thread Python
            pythonTaskQueue.CompleteAdding();
            pythonThread.Join();
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
            await ExecutePythonActionAsync(() =>
            {
                using (Py.GIL())
                {
                    executorInstance.app_instance = appInstance;
                    try
                    {
                        var result = executorInstance.initialSetup();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                    }

                    string output = executorInstance.log_stream.getvalue().ToString();
                    File.WriteAllText(".\\Python\\output.txt", output);
                }
            });
        }

        private async void Start_Stop_Click(object sender, RoutedEventArgs e)
        {
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

            await RunPythonScriptAsync(selectedAppInstance); // Executa o script Python de forma assíncrona
        }
    }
}
