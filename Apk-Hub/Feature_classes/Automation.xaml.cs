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
    /// <summary>
    /// Classe responsável por gerenciar a automação de testes em dispositivos Android.
    /// Controla a execução de scripts Python e a interação com o AirTest.
    /// </summary>
    public partial class Automation : Window, IComponentConnector
    {
        #region Fields and Properties
        private readonly MainWindow mainWindow = MainWindow.Instance;
        private dynamic executorInstance;
        private dynamic sys;
        private DispatcherTimer outputTimer;
        private readonly StringBuilder outputBuffer = new StringBuilder();
        private bool isRunning = false;
        private BlockingCollection<Action> pythonTaskQueue;
        private CancellationTokenSource cancellationTokenSource;
        private TaskCompletionSource<bool> initializationCompletionSource;
        private Thread pythonThread;
        private readonly string serialno;

        private static readonly Dictionary<string, (string PackageName, string DisplayName, string Instance)> appStringMap = new Dictionary<string, (string, string, string)>
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
        #endregion

        /// <summary>
        /// Construtor da classe Automation.
        /// Inicializa a janela e configura o ambiente Python para execução dos testes.
        /// </summary>
        /// <param name="device">Serial number do dispositivo Android alvo</param>
        public Automation(string device)
        {
            InitializeComponent();
            serialno = device;
            ConfigureWindow();
            SetupPythonEnvironment();
            InitializePythonThread();
        }

        #region Initialization Methods
        /// <summary>
        /// Configura as propriedades iniciais da janela.
        /// Define o owner e a posição inicial.
        /// </summary>
        private void ConfigureWindow()
        {
            Owner = mainWindow;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        /// <summary>
        /// Configura o ambiente Python procurando pela instalação mais recente
        /// e configurando o caminho do DLL apropriado.
        /// </summary>
        private static void SetupPythonEnvironment()
        {
            string localapp = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Programs",
                "Python"
            );

            string[] pythonVersions = { "Python312", "Python311", "Python310" };
            bool pythonFound = false;
            
            foreach (var version in pythonVersions)
            {
                string pythonPath = Path.Combine(localapp, version);
                string dllPath = Path.Combine(pythonPath, $"{version.ToLower()}.dll");
                
                if (Directory.Exists(pythonPath) && File.Exists(dllPath))
                {
                    Runtime.PythonDLL = dllPath;
                    pythonFound = true;
                    break;
                }
            }

            if (!pythonFound)
            {
                throw new InvalidOperationException(
                    "Python runtime não encontrado. Por favor, instale Python 3.10-3.12 em: " +
                    $"{localapp}\\Python3xx\\"
                );
            }
        }
        #endregion

        #region Python Thread Management
        /// <summary>
        /// Inicializa a thread Python para execução dos testes.
        /// </summary>
        private void InitializePythonThread()
        {
            ResetPythonState();
            ConfigurePythonThread();
            StartPythonThread();
        }

        /// <summary>
        /// Reseta o estado da thread Python.
        /// </summary>
        private void ResetPythonState()
        {
            cancellationTokenSource = new CancellationTokenSource();
            pythonTaskQueue = new BlockingCollection<Action>();
            initializationCompletionSource = new TaskCompletionSource<bool>();
            Application.Current.Dispatcher.Invoke(() => ToogleElements(AutomationGrid, false));
        }

        /// <summary>
        /// Configura a thread Python para execução dos testes.
        /// </summary>
        private void ConfigurePythonThread()
        {
            pythonThread = new Thread(() =>
            {
                try
                {
                    InitializePythonRuntime();
                    RunPythonMainLoop();
                }
                finally
                {
                    CleanupPythonResources();
                }
            });
        }

        /// <summary>
        /// Inicializa o runtime Python e configura os componentes necessários.
        /// </summary>
        private void InitializePythonRuntime()
        {
            PythonEngine.Initialize();
            using (Py.GIL())
            {
                UpdateStatus("Initializing Python AirTest, please wait...");
                InitializePythonComponents();
                UpdateDeviceInfo();
            }
            initializationCompletionSource.TrySetResult(true);
        }

        /// <summary>
        /// Inicializa os componentes Python necessários para a execução dos testes.
        /// </summary>
        private void InitializePythonComponents()
        {
            sys = Py.Import("sys");
            sys.path.append(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Python\\scripts"));
            dynamic pythonScript = Py.Import("executor");
            executorInstance = pythonScript.Executor(serialno);
            executorInstance.new_request();
        }

        /// <summary>
        /// Atualiza as informações do dispositivo.
        /// </summary>
        private void UpdateDeviceInfo()
        {
            string model = executorInstance.model.ToString();
            string androidVersion = executorInstance.osVersion.ToString();
            string buildMode = executorInstance.build.ToString();
            string themeMode = executorInstance.themeMode.ToString();
            PyTuple res = executorInstance.res;
            string resolution = $"{res[0]} x {res[1]}";

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

        /// <summary>
        /// Executa o loop principal da thread Python.
        /// </summary>
        private void RunPythonMainLoop()
        {
            while (!pythonTaskQueue.IsCompleted)
            {
                try
                {
                    if (pythonTaskQueue.TryTake(out var task, Timeout.Infinite, cancellationTokenSource.Token))
                    {
                        task();
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Limpa os recursos Python após a execução dos testes.
        /// </summary>
        private void CleanupPythonResources()
        {
            PythonEngine.Shutdown();
            if (IsVisible)
            {
                UpdateStatus("Python stopped");
            }
            isRunning = false;
        }

        /// <summary>
        /// Inicia a thread Python.
        /// </summary>
        private void StartPythonThread()
        {
            pythonThread.Start();
        }
        #endregion

        #region Automation Control
        /// <summary>
        /// Executa o script Python de automação.
        /// </summary>
        /// <param name="appInstance">Instância do aplicativo a ser testado</param>
        /// <param name="selectedSettings">Configurações selecionadas para o teste</param>
        /// <returns>Tarefa que representa a execução do script</returns>
        private async Task RunPythonScriptAsync(List<string> appInstance, List<string>? selectedSettings)
        {
            try
            {
                var (report, isError) = await ExecuteAutomationScript(appInstance, selectedSettings);
                await HandleAutomationResults(report, isError);
            }
            catch (Exception ex)
            {
                UpdateStatus($"Erro na execução do teste: {ex.Message}", Brushes.Red);
                ToogleElements(AutomationGrid, true);
            }
        }

        /// <summary>
        /// Executa o script Python de automação e retorna o resultado.
        /// </summary>
        /// <param name="appInstance">Instância do aplicativo a ser testado</param>
        /// <param name="selectedSettings">Configurações selecionadas para o teste</param>
        /// <returns>Tupla contendo o relatório do teste e um booleano indicando se houve erro</returns>
        private async Task<(string report, bool isError)> ExecuteAutomationScript(List<string> appInstance, List<string>? selectedSettings)
        {
            string report = string.Empty;
            bool isError = false;

            InitializeAutomationUI();
            StartOutputTimer();

            try
            {
                await ExecutePythonActionAsync(() =>
                {
                    using (Py.GIL())
                    {
                        if (cancellationTokenSource.Token.IsCancellationRequested) return;

                        executorInstance.app_instance = appInstance;
                        if (selectedSettings != null)
                        {
                            executorInstance.selected_settings = selectedSettings;
                        }

                        executorInstance.initialSetup();
                        report = executorInstance.logname.ToString();
                    }
                });
            }
            catch
            {
                isError = true;
            }
            finally
            {
                Application.Current.Dispatcher.Invoke(() => outputTimer.Stop());
            }

            return (report, isError);
        }

        /// <summary>
        /// Inicializa a interface de usuário para a execução do teste.
        /// </summary>
        private void InitializeAutomationUI()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                StatusText.Text = string.Empty;
                StatusText.Foreground = Brushes.White;
            });
        }

        /// <summary>
        /// Inicia o timer para atualizar a saída do teste.
        /// </summary>
        private void StartOutputTimer()
        {
            outputTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            outputTimer.Tick += (s, e) => HandleAutomationOutput();
            outputTimer.Start();
        }

        /// <summary>
        /// Trata o resultado do teste e atualiza a interface de usuário.
        /// </summary>
        /// <param name="report">Relatório do teste</param>
        /// <param name="isError">Booleano indicando se houve erro</param>
        /// <returns>Tarefa que representa a atualização da interface de usuário</returns>
        private async Task HandleAutomationResults(string report, bool isError)
        {
            UpdateAutomationState();
            await SaveAutomationOutput();

            if (!isError && !report.Contains("None") && IsVisible)
            {
                await ShowSuccessDialog(report);
            }
            else if (IsVisible)
            {
                ShowErrorDialog();
            }

            ToogleElements(AutomationGrid, true);
        }

        /// <summary>
        /// Atualiza o estado da automação.
        /// </summary>
        private void UpdateAutomationState()
        {
            isRunning = false;
            Application.Current.Dispatcher.Invoke(() =>
            {
                Start_Stop.Content = "Start";
                Start_Stop.Background = Brushes.Green;
            });
        }

        /// <summary>
        /// Salva a saída do teste em um arquivo.
        /// </summary>
        /// <returns>Tarefa que representa a gravação do arquivo</returns>
        private async Task SaveAutomationOutput()
        {
            string path = Path.Combine(mainWindow.appPath, "Output");
            Directory.CreateDirectory(path);
            string outputPath = Path.Combine(path, $"{DateTime.Now:yyMMdd_HHmmss}_OutputAutomation.txt");
            await File.WriteAllTextAsync(outputPath, StatusText.Text);
        }

        /// <summary>
        /// Exibe um diálogo de sucesso após a execução do teste.
        /// </summary>
        /// <param name="report">Relatório do teste</param>
        /// <returns>Tarefa que representa a exibição do diálogo</returns>
        private async Task ShowSuccessDialog(string report)
        {
            var result = MessageBox.Show(
                "Do you want to check the results",
                "Automation Finished",
                MessageBoxButton.YesNo,
                MessageBoxImage.Information
            );

            if (result == MessageBoxResult.Yes)
            {
                await Task.Run(() =>
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = Path.Combine(report, "log.html"),
                        UseShellExecute = true,
                        Verb = "open"
                    });
                });
            }
        }

        /// <summary>
        /// Exibe um diálogo de erro após a execução do teste.
        /// </summary>
        private void ShowErrorDialog()
        {
            MessageBox.Show(
                "For some reason the automation was not executed, check the log for more info",
                "Error found",
                MessageBoxButton.OK,
                MessageBoxImage.Warning
            );
        }

        /// <summary>
        /// Para a execução do teste.
        /// </summary>
        /// <returns>Tarefa que representa a parada do teste</returns>
        public async Task StopPythonExecution()
        {
            if (!isRunning) return;

            await StopAutomationProcess();
            await CleanupPythonTasks();
        }

        /// <summary>
        /// Para o processo de automação.
        /// </summary>
        /// <returns>Tarefa que representa a parada do processo</returns>
        private async Task StopAutomationProcess()
        {
            try
            {
                outputTimer?.Stop();
                using (Py.GIL())
                {
                    if (executorInstance != null)
                    {
                        executorInstance.cancellation_request();
                    }
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Erro ao parar automação: {ex.Message}", Brushes.Red);
            }
        }

        /// <summary>
        /// Limpa as tarefas Python após a parada do teste.
        /// </summary>
        /// <returns>Tarefa que representa a limpeza das tarefas</returns>
        private async Task CleanupPythonTasks()
        {
            await Task.Run(() =>
            {
                try
                {
                    if (!pythonTaskQueue.IsAddingCompleted)
                    {
                        pythonTaskQueue.CompleteAdding();
                    }
                    cancellationTokenSource.Cancel();

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        ToogleElements(AutomationGrid, false);
                        UpdateStatus("Waiting python to be stopped...", Brushes.Red);
                    });
                }
                catch (Exception ex)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                        UpdateStatus($"Erro ao limpar recursos: {ex.Message}", Brushes.Red)
                    );
                }
            });
        }

        /// <summary>
        /// Trata o clique no botão de início/parada.
        /// </summary>
        /// <param name="sender">Objeto que disparou o evento</param>
        /// <param name="e">Argumentos do evento</param>
        private async void Start_Stop_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button) return;

            if (isRunning)
            {
                await HandleStop(button);
            }
            else
            {
                await HandleStart(button);
            }
        }

        /// <summary>
        /// Trata a parada do teste.
        /// </summary>
        /// <param name="button">Botão que disparou o evento</param>
        /// <returns>Tarefa que representa a parada do teste</returns>
        private async Task HandleStop(Button button)
        {
            using (Py.GIL())
            {
                executorInstance.cancellation_request();
            }
            await StopPythonExecution();
            UpdateButtonState(button, "Start", Brushes.Green);
        }

        /// <summary>
        /// Trata o início do teste.
        /// </summary>
        /// <param name="button">Botão que disparou o evento</param>
        /// <returns>Tarefa que representa o início do teste</returns>
        private async Task HandleStart(Button button)
        {
            if (cancellationTokenSource.IsCancellationRequested)
            {
                InitializePythonThread();
            }

            await initializationCompletionSource.Task;

            var selectedApps = GetSelectedApps();
            var selectedSettings = GetSettingsSelected(Stack_Settings);

            UpdateButtonState(button, "Stop", Brushes.Red);
            isRunning = true;

            await RunPythonScriptAsync(selectedApps.Instance, selectedSettings);
        }

        /// <summary>
        /// Atualiza o estado do botão de início/parada.
        /// </summary>
        /// <param name="button">Botão a ser atualizado</param>
        /// <param name="content">Conteúdo do botão</param>
        /// <param name="background">Cor de fundo do botão</param>
        private void UpdateButtonState(Button button, string content, Brush background)
        {
            button.Content = content;
            button.Background = background;
        }
        #endregion

        #region UI Management
        /// <summary>
        /// Atualiza o status da interface de usuário.
        /// </summary>
        /// <param name="message">Mensagem a ser exibida</param>
        /// <param name="color">Cor da mensagem</param>
        private void UpdateStatus(string message, Brush? color = null)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                StatusText.Text = message;
                if (color != null)
                {
                    StatusText.Foreground = color;
                }
            });
        }

        /// <summary>
        /// Anexa uma mensagem ao status da interface de usuário.
        /// </summary>
        /// <param name="message">Mensagem a ser anexada</param>
        private void AppendStatus(string message)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                StatusText.Text += message;
                StatusText.ScrollToEnd();
            });
        }

        /// <summary>
        /// Trata a saída do teste e atualiza a interface de usuário.
        /// </summary>
        /// <returns>Tarefa que representa a atualização da interface de usuário</returns>
        private async Task HandleAutomationOutput()
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                await ReadLogOutput(cts.Token);
            }
            catch (OperationCanceledException)
            {
                AppendStatus("...");
            }
            catch (Exception ex)
            {
                AppendStatus($"Erro: {ex.Message}");
            }
        }

        /// <summary>
        /// Lê a saída do teste e atualiza a interface de usuário.
        /// </summary>
        /// <returns>Tarefa que representa a leitura da saída</returns>
        private async Task ReadLogOutput(CancellationToken cancellationToken)
        {
            await Task.Run(() =>
            {
                try
                {
                    if (executorInstance?.log_stream == null) return;

                    string log_output = executorInstance.log_stream.getvalue();
                    if (!string.IsNullOrEmpty(log_output))
                    {
                        AppendStatus(log_output);
                        executorInstance.log_stream.truncate(0);
                        executorInstance.log_stream.seek(0);
                    }
                }
                catch (Exception ex)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                        AppendStatus($"Erro ao ler log: {ex.Message}")
                    );
                }
            }, cancellationToken);
        }
        #endregion

        /// <summary>
        /// Habilita ou desabilita todos os elementos de UI dentro de um painel.
        /// Útil para bloquear interações durante processamentos.
        /// </summary>
        /// <param name="panel">Painel contendo os elementos a serem alterados</param>
        /// <param name="isEnabled">True para habilitar, False para desabilitar</param>
        private void ToogleElements(Panel panel, bool isEnabled)
        {
            foreach (var control in panel.Children)
            {
                if (control is StackPanel stackpanel)
                {
                    stackpanel.IsEnabled = isEnabled;
                }
                else if (control is UniformGrid uniformgrid)
                {
                    uniformgrid.IsEnabled = isEnabled;
                }
            }
        }

        /// <summary>
        /// Executa uma ação Python na thread Python.
        /// </summary>
        /// <param name="pythonAction">Ação Python a ser executada</param>
        /// <returns>Tarefa que representa a execução da ação</returns>
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

        /// <summary>
        /// Obtém as configurações selecionadas para o teste.
        /// </summary>
        /// <param name="container">Contêiner contendo as configurações</param>
        /// <returns>Lista de configurações selecionadas</returns>
        public List<string>? GetSettingsSelected(StackPanel container)
        {
            List<string> selectedSettings = new List<string>();

            foreach (var child in container.Children)
            {
                if (child is CheckBox checkBox && checkBox.IsChecked == true)
                {
                    selectedSettings.Add(checkBox.Name);
                }
            }

            return selectedSettings.Count > 0 ? selectedSettings : null;
        }

        /// <summary>
        /// Obtém os aplicativos selecionados para teste.
        /// </summary>
        /// <returns>Tupla contendo listas de valores, nomes e instâncias dos aplicativos selecionados</returns>
        private (List<string> Values, List<string> Names, List<string> Instance) GetSelectedApps()
        {
            var values = new List<string>();
            var names = new List<string>();
            var instances = new List<string>();

            foreach (var child in AppsStackPanel.Children)
            {
                if (child is CheckBox { IsChecked: true } checkBox)
                {
                    string content = checkBox.Content.ToString();
                    if (appStringMap.TryGetValue(content, out var value))
                    {
                        values.Add(value.PackageName);
                        names.Add(value.DisplayName);
                        instances.Add(value.Instance);
                    }
                }
            }

            return (values, names, instances);
        }
    }
}
