using ApkInstaller.Feature_classes;
using ApkInstaller.Helper_classes;
using MahApps.Metro.Controls;
using Microsoft.Win32;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Media;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;

namespace ApkInstaller;

public class DeviceInfo
{
    public string AndroidVersion { get; set; } = string.Empty;
    public string BuildMode { get; set; } = string.Empty;
    public string CscCode { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public string SdkVersion { get; set; } = string.Empty;
    public string DeviceSelected { get; set; } = string.Empty;
    public string SerialNo {  get; set; } = string.Empty;


    public override string ToString() => $"{DeviceName} ({DeviceSelected})";
}

public partial class MainWindow : MetroWindow, IComponentConnector
{
    #region Fields and Properties
    private Settings? settingsWindow;
    private Kids? kidsWindow;
    private UsbDeviceNotifier? usbDeviceNotifier;
    public More? moreWindow;
    private DeviceInformation? deviceInfoWindow;
    private bool loopCancelation = false;
    private bool success;
    private readonly List<Window> childWindows = new();
    private List<string> ipDevices = [];
    private readonly SemaphoreSlim checkDevicesThread = new SemaphoreSlim(1, 1);

    /// <summary>
    /// Caminho para o diretório de logs do aplicativo
    /// </summary>
    public string appPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}\\ApkHub\\Log";

    /// <summary>
    /// Instância singleton da janela principal
    /// </summary>
    public static MainWindow Instance { get; private set; } = null!;
    #endregion

    #region Initialization
    /// <summary>
    /// Construtor da janela principal
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();
        ConfigureWindowAppearance();
        Instance = this;
        base.Loaded += MainWindow_Loaded;
        base.Closing += MainWindow_Closing;
    }

    /// <summary>
    /// Configura a aparência inicial da janela
    /// </summary>
    private void ConfigureWindowAppearance()
    {
        SolidColorBrush appColor = new(Color.FromRgb(107, 16, 245));
        this.WindowTitleBrush = appColor;
        this.BorderBrush = appColor;
        Install_Button.Content = "Install APKs";
    }

    /// <summary>
    /// Manipulador do evento Loaded da janela
    /// </summary>
    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await PopulateDevices();
        Directory.CreateDirectory(appPath);
        InitializeUsbDeviceNotifier();
    }

    /// <summary>
    /// Inicializa o notificador de dispositivos USB
    /// </summary>
    private void InitializeUsbDeviceNotifier()
    {
        usbDeviceNotifier = new UsbDeviceNotifier(this);
        usbDeviceNotifier.UsbDeviceChanged += OnUsbDeviceChanged!;
    }
    #endregion

    #region Device Management
    /// <summary>
    /// Manipulador do evento de mudança de dispositivo USB
    /// </summary>
    private async void OnUsbDeviceChanged(object sender, EventArgs e)
    {
        await PopulateDevices();
    }

    /// <summary>
    /// Popula a lista de dispositivos conectados
    /// </summary>
    public async Task PopulateDevices()
    {
        if (!await checkDevicesThread.WaitAsync(0)) return;
        ipDevices = [];
        EnableDevicesBox(false);
        Button_Status([AddIpDevice, Browse_Button, More_Button, Kids_Button, ParentalCare_Button, RemoveIpDevice , DeviceInfo_Button], [false, false, false, false, false, false, false]);
        var deviceSelected = DevicesComboBox.SelectedItem as DeviceInfo;
        Install_Button.IsEnabled = false;
        UpdateStatusText("Checking device(s) connected...", clear: true);

        var deviceList = new List<DeviceInfo>();
        var connectedDevices = await Task.Run(GetConnectedDevices);
        await Task.Delay(1500);

        foreach (var device in connectedDevices)
        {
            if (device.Key.Contains(':')) ipDevices.Add(device.Key);
            var tasks = new List<Task<string>>()
            {
                Task.Run(() => AdbHelper.Instance.GetAdbReturn($"getprop ro.build.version.release", device.Key, true)),
                Task.Run(() => AdbHelper.Instance.GetAdbReturn($"getprop ro.build.type", device.Key, true)),
                Task.Run(() => AdbHelper.Instance.GetAdbReturn($"getprop ro.omc.multi_csc", device.Key, true)),
                Task.Run(() => AdbHelper.Instance.GetAdbReturn($"getprop ro.system.build.version.sdk", device.Key, true)),
                Task.Run(() => AdbHelper.Instance.GetAdbReturn($"getprop ro.product.manufacturer", device.Key, true)),
                Task.Run(() => AdbHelper.Instance.GetAdbReturn($"getprop ro.serialno", device.Key, true))
            };
            var results = await Task.WhenAll(tasks);
            string androidVersion = results[0];
            string buildMode = results[1];
            string cscCode = results[2];
            string sdkVersion = results[3];
            string manufacturer = results[4];
            string serialno = results[5];

            deviceList.Add(new DeviceInfo
            {
                AndroidVersion = androidVersion,
                BuildMode = buildMode.ToUpper(),
                CscCode = !string.IsNullOrEmpty(cscCode) ? cscCode : "Not found",
                DeviceName = device.Value,
                Manufacturer = string.IsNullOrEmpty(manufacturer) ? "Not found" : char.ToUpper(manufacturer[0]) + manufacturer[1..].ToLower(),
                SdkVersion = string.IsNullOrEmpty(sdkVersion) ? "Not found" : sdkVersion,
                DeviceSelected = device.Key,
                SerialNo = serialno

            });
        }
        deviceSelected = deviceList.FirstOrDefault(d =>
            d.DeviceSelected == deviceSelected?.DeviceSelected &&
            d.DeviceName == deviceSelected.DeviceName
        ) ?? null;

        Dispatcher.Invoke(() =>
        {
            DevicesComboBox.ItemsSource = deviceList;
            DevicesComboBox.SelectedItem = deviceList.Count == 1 ? deviceList[0] : deviceSelected;
            DisableEnable_SamsungDevices(DevicesComboBox.SelectedItem as DeviceInfo);
            Button_Status([AddIpDevice , Browse_Button, More_Button, DeviceInfo_Button], [true, true, true, true]);

            int count = deviceList.Count;
            UpdateStatusText(count > 0 ? $"{count} Device(s) Connected" : "No Device Connected", count == 0, count > 0, clear: true);

            AllowInstall();
            if (count > 1 && !Application.Current.Windows.OfType<Window>().Any(w =>
            w != Application.Current.MainWindow &&
            w.GetType().Namespace != "Microsoft.VisualStudio.DesignTools.WpfTap.WpfVisualTreeService.Adorners"
            )) EnableDevicesBox();

            if (DevicesComboBox.SelectedItem == null)
            {
                foreach (var window in Application.Current.Windows.OfType<Window>().Where(w => w != Application.Current.MainWindow))
                {
                    window.Close();
                }
            }
        });
        RemoveIpDevice.IsEnabled = ipDevices.Count > 0;
        checkDevicesThread.Release();
    }

    /// <summary>
    /// Obtém a lista de dispositivos conectados via ADB
    /// </summary>
    /// <returns>Dicionário com o serial e nome dos dispositivos conectados</returns>
    private async Task<Dictionary<string, string>> GetConnectedDevices()
    {
        string devices = string.Empty;
        Dictionary<string, string> dictionary = [];
        await AdbHelper.Instance.RunAdbCommandAsync("devices", output => { devices += $"\n{output}"; }, generalCommand: true);
        using StringReader stringReader = new (devices);
        string? text;
        while ((text = stringReader.ReadLine()) != null)
        {
            if (text.EndsWith("device"))
            {
                string text2 = text.Split('\t')[0];
                string deviceName = await GetDeviceName(text2);
                dictionary.Add(text2, deviceName);
            }
        }
        return dictionary;
    }

    /// <summary>
    /// Obtém o nome do dispositivo a partir do seu número serial
    /// </summary>
    /// <param name="serial">Número serial do dispositivo</param>
    /// <returns>Nome do dispositivo</returns>
    public async Task<string> GetDeviceName(string serial)
    {
        string name = await AdbHelper.Instance.GetAdbReturn("getprop ro.product.model", serial, true);
        return name.Split('\n')[0].Trim();
    }

    /// <summary>
    /// Obtém o número serial do dispositivo a partir do seu nome
    /// </summary>
    /// <param name="name">Nome do dispositivo no formato "nome (serial)"</param>
    /// <returns>Número serial do dispositivo</returns>
    public string GetDeviceSerialByName(string name)
    {
        foreach (object item3 in (IEnumerable)DevicesComboBox.Items)
        {
            if (!(item3.ToString() == name))
            {
                continue;
            }
            foreach (object item2 in DevicesComboBox.ItemsSource)
            {
                if (item2.ToString() == name)
                {
                    return item2.ToString()!.Split('(')[1].Trim(' ', ')');
                }
            }
        }
        return null!;
    }

    private async void ConnectDevice_Click(object sender, RoutedEventArgs e)
    {
        string ipDevice = Microsoft.VisualBasic.Interaction.InputBox(
                "Enter the ip of the device to connect (Ip:Port):\nThe device must be in the same network as the computer.", "Connect an IP device", string.Empty);
        if (!string.IsNullOrEmpty(ipDevice)) {
            UpdateStatusText($"Attemping to connect with {ipDevice}", clear: true);
            string response = await AdbHelper.Instance.GetAdbReturn($"connect {ipDevice}",generalCommand: true);
            if (response.Contains("connected to"))
            {
                await PopulateDevices();
            }
            else
            {
                UpdateStatusText($"Device not connected, check the IP:Port address and try again.\nResponse: {response}", isError: true, clear: true);
            }
        }
    }

    private async void RemoveIpDevice_Click(object sender, RoutedEventArgs e)
    {
        await DisconnectIpDevices();
    }

    public async Task DisconnectIpDevices()
    {

        UpdateStatusText(clear: true);
        foreach (var device in ipDevices)
        {
            string result = await AdbHelper.Instance.GetAdbReturn($"disconnect {device}", generalCommand: true);
            if (result.Contains("disconnected"))
            {
                await PopulateDevices();
                UpdateStatusText($"The device was {result}", isSuccess: true);
            }
            else UpdateStatusText(result, isError: true, clear: true);
        }
    }
    #endregion

    #region APK File Management
    /// <summary>
    /// Manipula o clique no botão de navegação
    /// </summary>
    private void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        ChangeButtonVisibility(false);
        var (errorMessages, addedFiles) = BrowseAndAddApkFiles();
        CheckDuplicatedFiles(errorMessages, addedFiles);
        ChangeButtonVisibility(true);
    }

    /// <summary>
    /// Abre o diálogo de seleção de arquivos e adiciona os APKs selecionados
    /// </summary>
    private (List<string> errorMessages, List<string> addedFiles) BrowseAndAddApkFiles()
    {
        var errorMessages = new List<string>();
        var addedFiles = new List<string>();

        var openFileDialog = new OpenFileDialog
        {
            Multiselect = true,
            Filter = "APK files (*.apk)|*.apk"
        };

        if (openFileDialog.ShowDialog() == true)
        {
            UpdateStatusText("Loading file(s) selected", clear: true);
            ProcessSelectedFiles(openFileDialog.FileNames, errorMessages, addedFiles);
            UpdateStatusText("File(s) loaded", clear: true);
        }
        else
        {
            UpdateStatusText("Action cancelled", clear: true);
        }

        return (errorMessages, addedFiles);
    }

    /// <summary>
    /// Processa os arquivos selecionados
    /// </summary>
    /// <param name="files">Array com os caminhos dos arquivos selecionados</param>
    /// <param name="errorMessages">Lista para armazenar mensagens de erro</param>
    /// <param name="addedFiles">Lista para armazenar arquivos adicionados com sucesso</param>
    private void ProcessSelectedFiles(string[] files, List<string> errorMessages, List<string> addedFiles)
    {
        foreach (var filename in files)
        {
            string error = AddApkFile(filename);
            string fileNameOnly = Path.GetFileName(filename);

            if (!string.IsNullOrEmpty(error))
            {
                errorMessages.Add(fileNameOnly);
            }
            else
            {
                addedFiles.Add(fileNameOnly);
            }
        }
    }

    /// <summary>
    /// Verifica e reporta arquivos duplicados
    /// </summary>
    /// <param name="errorMessages">Lista de mensagens de erro dos arquivos duplicados</param>
    /// <param name="addedFiles">Lista de arquivos adicionados com sucesso</param>
    private void CheckDuplicatedFiles(List<string> errorMessages, List<string> addedFiles)
    {
        if (errorMessages.Any())
        {
            ShowDuplicateFilesMessage(errorMessages, addedFiles);
            UpdateStatusText("Duplicated file(s)", clear: true, isError: true);
            return;
        }
    }

    /// <summary>
    /// Exibe mensagem sobre arquivos duplicados
    /// </summary>
    /// <param name="errorMessages">Lista de mensagens de erro dos arquivos duplicados</param>
    /// <param name="addedFiles">Lista de arquivos adicionados com sucesso</param>
    private void ShowDuplicateFilesMessage(List<string> errorMessages, List<string> addedFiles)
    {
        string errorMessage = CreateDuplicateErrorMessage(errorMessages);

        if (addedFiles.Any())
        {
            errorMessage += $"\n\n{CreateAddedFilesMessage(addedFiles)}";
        }

        ShowMessage(errorMessage, "File Selection Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    /// <summary>
    /// Cria mensagem de erro para arquivos duplicados
    /// </summary>
    /// <param name="errorMessages">Lista de mensagens de erro dos arquivos duplicados</param>
    /// <returns>Mensagem formatada com os erros</returns>
    private string CreateDuplicateErrorMessage(List<string> errorMessages)
    {
        return errorMessages.Count == 1
            ? $"The following file:\n'{string.Join("', '", errorMessages)}' is already selected, please remove it and try again."
            : $"The following files:\n'{string.Join("', '", errorMessages)}' are already selected, please remove them and try again.";
    }

    /// <summary>
    /// Cria mensagem para arquivos adicionados com sucesso
    /// </summary>
    /// <param name="addedFiles">Lista de arquivos adicionados com sucesso</param>
    /// <returns>Mensagem formatada com os arquivos adicionados</returns>
    private string CreateAddedFilesMessage(List<string> addedFiles)
    {
        return addedFiles.Count == 1
            ? $"Additionally, the following file was successfully added:\n'{string.Join("', '", addedFiles)}'"
            : $"Additionally, the following files were successfully added:\n'{string.Join("', '", addedFiles)}'";
    }

    /// <summary>
    /// Adiciona um arquivo APK à lista
    /// </summary>
    /// <param name="filename">Caminho completo do arquivo APK</param>
    /// <returns>String vazia se sucesso, ou mensagem de erro se falha</returns>
    private string AddApkFile(string filename)
    {
        if (IsApkAlreadyAdded(filename, out string error))
        {
            return error;
        }

        AddApkToList(filename);
        return string.Empty;
    }

    /// <summary>
    /// Verifica se um APK já está na lista
    /// </summary>
    /// <param name="filename">Caminho completo do arquivo APK</param>
    /// <param name="error">Mensagem de erro se o arquivo já existir</param>
    /// <returns>True se o arquivo já existe, False caso contrário</returns>
    private bool IsApkAlreadyAdded(string filename, out string error)
    {
        error = string.Empty;
        string fileName = Path.GetFileName(filename);

        foreach (StackPanel item in ApkFilesList.Items)
        {
            if (item.Children.OfType<TextBlock>().FirstOrDefault()?.Text == fileName)
            {
                error = fileName;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Adiciona um APK à lista de interface
    /// </summary>
    /// <param name="filename">Caminho completo do arquivo APK</param>
    private void AddApkToList(string filename)
    {
        var stackPanel = CreateApkListItem(filename);
        ApkFilesList.Items.Add(stackPanel);
        ChangeButtonVisibility(true);
    }

    /// <summary>
    /// Cria um item de lista para o APK
    /// </summary>
    /// <param name="filename">Caminho completo do arquivo APK</param>
    /// <returns>StackPanel contendo o item da lista</returns>
    private StackPanel CreateApkListItem(string filename)
    {
        var stackPanel = new StackPanel { Orientation = Orientation.Horizontal };
        var textBlock = new TextBlock
        {
            Text = Path.GetFileName(filename),
            Margin = new Thickness(0, 0, 10, 0)
        };
        var deleteButton = new Button
        {
            Content = "Delete",
            Tag = filename
        };
        deleteButton.Click += DeleteButton_Click;

        stackPanel.Children.Add(textBlock);
        stackPanel.Children.Add(deleteButton);
        stackPanel.Tag = filename;

        return stackPanel;
    }

    /// <summary>
    /// Manipula o arraste de arquivos sobre a lista
    /// </summary>
    private void Grid_DragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(DataFormats.FileDrop, autoConvert: false) is string[] files)
        {
            bool hasApkFile = files.Any(file => file.EndsWith(".apk"));
            this.AllowDrop = true;
            e.Effects = hasApkFile ? DragDropEffects.Copy : DragDropEffects.None;
        }
        e.Handled = true;
    }

    /// <summary>
    /// Manipula o soltar de arquivos na lista
    /// </summary>
    private void Grid_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(DataFormats.FileDrop, autoConvert: false) is string[] files)
        {
            var (errorMessages, addedFiles) = ProcessDroppedFiles(files);
            CheckDuplicatedFiles(errorMessages, addedFiles);
            AllowInstall();
            UpdateStatusText("File(s) loaded", clear: true);
        }
    }

    /// <summary>
    /// Processa os arquivos soltos na lista
    /// </summary>
    private (List<string> errorMessages, List<string> addedFiles) ProcessDroppedFiles(string[] files)
    {
        var errorMessages = new List<string>();
        var addedFiles = new List<string>();

        foreach (var filename in files.Where(f => f.EndsWith(".apk")))
        {
            string error = AddApkFile(filename);
            string fileNameOnly = Path.GetFileName(filename);

            if (!string.IsNullOrEmpty(error))
            {
                errorMessages.Add(fileNameOnly);
            }
            else
            {
                addedFiles.Add(fileNameOnly);
            }
        }

        return (errorMessages, addedFiles);
    }

    /// <summary>
    /// Manipula o clique no botão de exclusão
    /// </summary>
    private void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Parent is StackPanel stackPanel)
        {
            ApkFilesList.Items.Remove(stackPanel);
            Install_Button.IsEnabled = ApkFilesList.Items.Count > 0;
        }
    }

    /// <summary>
    /// Limpa a lista de APKs e o texto de saída
    /// </summary>
    private void EmptyOutput_Button(object sender, RoutedEventArgs e)
    {
        ApkFilesList.Items.Clear();
        UpdateStatusText(clear: true);
    }
    #endregion

    #region APK Installation
    /// <summary>
    /// Manipula o clique no botão de instalação
    /// </summary>
    private async void InstallButton_Click(object sender, RoutedEventArgs e)
    {
        string? device = CheckDeviceComboBox();
        if (device == null)
        {
            UpdateStatusText("Please select a device.", isError: true, clear: true);
            return;
        }

        if (Install_Button.Content.ToString() == "Install APKs")
        {
            await StartInstallation(device);
        }
        else
        {
            CancelInstallation();
        }
    }

    /// <summary>
    /// Inicia o processo de instalação dos APKs
    /// </summary>
    private async Task StartInstallation(string device)
    {
        ApkFilesList.IsEnabled = false;
        UpdateInstallButtonState(true);

        var apkFiles = GetSelectedApkFiles();
        string deviceSerial = GetDeviceSerialByName(device);
        await InstallApks(deviceSerial, apkFiles);
    }

    /// <summary>
    /// Cancela o processo de instalação
    /// </summary>
    private void CancelInstallation()
    {
        loopCancelation = true;
        success = false;
        AdbHelper.Instance.StopCommand();
        UpdateStatusText("Installation canceled", isError: true);
        ApkFilesList.IsEnabled = true;
        UpdateInstallButtonState(false);
    }

    /// <summary>
    /// Atualiza o estado do botão de instalação
    /// </summary>
    private void UpdateInstallButtonState(bool installing)
    {
        Install_Button.Content = installing ? "Stop" : "Install APKs";
        Install_Button.Background = installing ? Brushes.Red : Brushes.Green;
    }

    /// <summary>
    /// Obtém a lista de APKs selecionados
    /// </summary>
    private List<string> GetSelectedApkFiles()
    {
        return ApkFilesList.Items.OfType<StackPanel>()
                          .Select(panel => panel.Tag.ToString()!)
                          .ToList();
    }

    /// <summary>
    /// Instala os APKs no dispositivo
    /// </summary>
    private async Task InstallApks(string device, List<string> apkFiles)
    {
        UpdateStatusText("Initializing the installation", clear: true);

        try
        {
            success = true;
            foreach (string apkFile in apkFiles)
            {
                if (!loopCancelation)
                {
                    var (installSuccess, output) = await InstallSingleApk(device, apkFile);
                    if (!installSuccess)
                    {
                        loopCancelation = true;
                        success = false;
                        break;
                    }
                }
                else
                {
                    success = false;
                    break;
                }
            }

            UpdateInstallationResult();
            ResetInstallationState();
        }
        catch (Exception ex)
        {
            Dispatcher.Invoke(() => UpdateStatusText($"Error: {ex.Message}", isError: true));
        }
    }

    /// <summary>
    /// Instala um único APK
    /// </summary>
    private async Task<(bool success, string output)> InstallSingleApk(string device, string apkFile)
    {
        string outputResult = "";
        UpdateStatusText($"\nInstalling \"{apkFile}\"");

        await AdbHelper.Instance.RunAdbCommandAsync(
            $"install -r -d \"{apkFile}\"",
            output =>
            {
                Dispatcher.Invoke(() =>
                {
                    UpdateStatusText(output);
                    outputResult += output;
                });
            },
            device,
            shell: false
        );

        return (outputResult.Contains("Success"), outputResult);
    }

    /// <summary>
    /// Atualiza o resultado da instalação na UI
    /// </summary>
    private void UpdateInstallationResult()
    {
        Dispatcher.Invoke(() =>
        {
            UpdateStatusText(
                success ? "Installation complete" : "Installation not complete.",
                isError: !success,
                isSuccess: success
            );
        });
    }

    /// <summary>
    /// Reseta o estado após a instalação
    /// </summary>
    private void ResetInstallationState()
    {
        UpdateInstallButtonState(false);
        ApkFilesList.IsEnabled = true;
        loopCancelation = false;
    }
    #endregion

    #region Button Click Events
    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        await PopulateDevices();
    }

    /// <summary>
    /// Altera a visibilidade dos botões da interface
    /// </summary>
    /// <param name="change">True para habilitar, False para desabilitar</param>
    private void ChangeButtonVisibility(bool change)
    {
        foreach (var child in MainWindowGrid.Children.OfType<StackPanel>())
        {
            foreach (var stackChild in child.Children.OfType<Button>())
            {
                stackChild.IsEnabled = change;
            }
        }
        if (DevicesComboBox.SelectedItem is DeviceInfo selectedDevice) DisableEnable_SamsungDevices(selectedDevice);
        Install_Button.IsEnabled = DevicesComboBox.SelectedItem != null && change && ApkFilesList.Items.Count > 0;
    }

    #endregion

    #region Window Management
    /// <summary>
    /// Abre uma janela filha e gerencia seu ciclo de vida
    /// </summary>
    /// <typeparam name="T">Tipo da janela filha</typeparam>
    /// <param name="childWindow">Instância da janela filha</param>
    /// <returns>A janela filha passada como parâmetro</returns>
    private T OpenChildWindow<T>(T childWindow) where T : Window
    {
        childWindows.Add(childWindow);
        childWindow.Closed += (s, e) => childWindows.Remove(childWindow);
        return childWindow;
    }

    /// <summary>
    /// Posiciona e exibe uma janela filha
    /// </summary>
    /// <param name="window">Janela a ser exibida</param>
    /// <param name="extraWidth">Largura extra para posicionamento</param>
    /// <param name="otherWindow1">Primeira janela de referência opcional</param>
    /// <param name="otherWindow2">Segunda janela de referência opcional</param>
    private void ShowWindow(Window window, double extraWidth, Window opened)
    {
        var shouldCenterWindow = base.Left + base.Width + extraWidth >= SystemParameters.PrimaryScreenWidth || Application.Current.Windows.OfType<Window>().Any(w =>
            w != Application.Current.MainWindow && w!= opened &&
            w.GetType().Namespace != "Microsoft.VisualStudio.DesignTools.WpfTap.WpfVisualTreeService.Adorners"
            );

        if (shouldCenterWindow)
        {
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        }
        else
        {
            window.Left = base.Left + base.Width;
            window.Top = base.Top;
        }
        window.Show();
    }

    /// <summary>
    /// Manipula o fechamento de uma janela filha
    /// </summary>
    /// <param name="controlsToEnable">Controles a serem habilitados após o fechamento</param>
    private void HandleWindowClosed(params Control[] controlsToEnable)
    {
        foreach (var control in controlsToEnable)
        {
            control.IsEnabled = true;
        }
        this.Activate();
    }

    /// <summary>
    /// Manipula o fechamento da janela principal
    /// </summary>
    private async void MainWindow_Closing(object sender, CancelEventArgs e)
    {
        await CleanupOnClosing();
        CloseAllChildWindows();
        AdbHelper.Instance.StopCommand();
        Application.Current.Shutdown();
    }

    /// <summary>
    /// Limpa recursos ao fechar a aplicação
    /// </summary>
    private async Task CleanupOnClosing()
    {
#if !DEBUG
        await AdbHelper.Instance.RunAdbCommandAsync("kill-server", output => { }, generalCommand: true);
#endif
    }

    /// <summary>
    /// Fecha todas as janelas filhas
    /// </summary>
    private void CloseAllChildWindows()
    {
        var windowsToClose = new List<Window>(childWindows);
        foreach (var child in windowsToClose)
        {
            if (child.IsVisible)
            {
                child.Close();
            }
        }
    }
    #endregion

    #region UI Helpers
    /// <summary>
    /// Exibe uma mensagem para o usuário
    /// </summary>
    public MessageBoxResult ShowMessage(
        string message,
        string? title = null,
        MessageBoxButton button = MessageBoxButton.OK,
        MessageBoxImage icon = MessageBoxImage.Warning)
    {
        return MessageBox.Show(message, title, button, icon);
    }

    /// <summary>
    /// Atualiza o texto de status na interface
    /// </summary>
    public void UpdateStatusText(string? message = null, bool isError = false, bool isSuccess = false, bool isWarning = false, bool clear = false)
    {
        base.Dispatcher.Invoke(() =>
        {
            StatusText.Foreground = isError ? Brushes.Red : (isSuccess ? Brushes.Green : (isWarning ? Brushes.Yellow : Brushes.White));

            if (clear)
            {
                StatusText.Text = string.Empty;
            }

            if (!string.IsNullOrEmpty(message))
            {
                StatusText.Text += $"{message}{Environment.NewLine}";
            }

            StatusText.ScrollToEnd();
        });
    }

    /// <summary>
    /// Habilita ou desabilita botões
    /// </summary>
    /// <param name="buttons">Lista de botões a serem configurados</param>
    /// <param name="states">Lista de estados correspondentes aos botões</param>
    private static void Button_Status(List<Button> buttons, List<bool> states)
    {
        for (int i = 0; i < buttons.Count && i < states.Count; i++)
        {
            buttons[i].IsEnabled = states[i];
        }
    }

    /// <summary>
    /// Verifica se o botão de instalação pode ser habilitado
    /// </summary>
    private void AllowInstall()
    {
        Install_Button.IsEnabled = DevicesComboBox.SelectedItem != null && ApkFilesList.Items.Count > 0;
    }

    /// <summary>
    /// Verifica o dispositivo selecionado
    /// </summary>
    private string? CheckDeviceComboBox()
    {
        return DevicesComboBox.SelectedItem?.ToString();
    }
    #endregion

    #region Other Methods
    /// <summary>
    /// Abre a janela Kids e configura seus estados
    /// </summary>
    private void KidsWindow_Click(object sender, RoutedEventArgs e)
    {
        string? device = CheckDeviceComboBox();
        if (device == null)
        {
            ShowMessage("Please select a device before opening Kids options.", "No Device Selected", MessageBoxButton.OK, MessageBoxImage.Exclamation);
        }
        else if (kidsWindow == null || !kidsWindow.IsVisible)
        {
            kidsWindow = OpenChildWindow(new Kids(this, GetDeviceSerialByName(device)));
            ShowWindow(kidsWindow, 250, kidsWindow);
            Button_Status([Install_Button, ParentalCare_Button, Browse_Button], [false, false, false]);
            DevicesComboBox.IsEnabled = false;
            kidsWindow.Closed += KidsWindow_Closed;
        }
        else
        {
            SystemSounds.Exclamation.Play();
            kidsWindow.Activate();
        }
    }

    /// <summary>
    /// Manipula o evento de fechamento da janela Kids
    /// </summary>
    private void KidsWindow_Closed(object? sender, EventArgs e)
    {
        if (ApkFilesList.Items.Count > 0)
        {
            HandleWindowClosed(Install_Button);
        }
        AdbHelper.Instance.StopCommand();
        kidsWindow = null;
    }

    /// <summary>
    /// Abre a janela de Controle Parental e configura seus estados
    /// </summary>
    private void PCWindow_Click(object sender, RoutedEventArgs e)
    {
        string? device = CheckDeviceComboBox();
        if (device == null)
        {
            ShowMessage("Please select a device before opening Parental Care options.", "No Device Selected", MessageBoxButton.OK, MessageBoxImage.Exclamation);
        }
        else if (settingsWindow == null || !settingsWindow.IsVisible)
        {
            settingsWindow = OpenChildWindow(new Settings(this, GetDeviceSerialByName(device)));
            ShowWindow(settingsWindow, 220, settingsWindow);
            ApkFilesList.IsEnabled = false;
            Button_Status([Install_Button, Kids_Button, Browse_Button], [false, false, false]);
            DevicesComboBox.IsEnabled = false;
            settingsWindow.Closed += PcWindow_Closed;
        }
        else
        {
            SystemSounds.Exclamation.Play();
            settingsWindow.Activate();
        }
    }

    /// <summary>
    /// Manipula o evento de fechamento da janela de Controle Parental
    /// </summary>
    private void PcWindow_Closed(object? sender, EventArgs e)
    {
        if (ApkFilesList.Items.Count > 0)
        {
            HandleWindowClosed(ApkFilesList, Install_Button);
        }
        AdbHelper.Instance.StopCommand();
        settingsWindow = null;
    }

    /// <summary>
    /// Abre a janela More e configura seus estados
    /// </summary>
    private void More_Button_Click(object sender, RoutedEventArgs e)
    {
        string? device = CheckDeviceComboBox();
        if (device == null)
        {
            ShowMessage("Please select a device before opening more options.", "No Device Selected", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            return;
        }
        else if (moreWindow == null)
        {
            moreWindow = OpenChildWindow(new More(this, GetDeviceSerialByName(device), settingsWindow, kidsWindow));
            ShowWindow(moreWindow, 250, moreWindow);
            DevicesComboBox.IsEnabled = false;
            moreWindow.Closed += MoreWindow_Closed;
        }
        else
        {
            SystemSounds.Exclamation.Play();
            moreWindow.Activate();
        }
    }

    /// <summary>
    /// Manipula o evento de fechamento da janela More
    /// </summary>
    private void MoreWindow_Closed(object? sender, EventArgs e)
    {
        EnableDevicesBox();
        moreWindow = null;
        AdbHelper.Instance.StopCommand();
        this.Activate();
    }


    private void InfoDevice_Click(object sender, RoutedEventArgs e)
    {
        DeviceInfo? device = DevicesComboBox.SelectedItem as DeviceInfo;
        if (device == null)
        {
            ShowMessage("Please select a device before opening the device information.", "No Device Selected", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            return;
        }
        else if (deviceInfoWindow == null)
        {
            deviceInfoWindow = OpenChildWindow(new DeviceInformation(this, device));
            ShowWindow(deviceInfoWindow, 250, deviceInfoWindow);
            DevicesComboBox.IsEnabled = false;
            deviceInfoWindow.Closed += DeviceInfoWindow_Closed; ;
        }
        else
        {
            SystemSounds.Exclamation.Play();
            deviceInfoWindow.Activate();
        }
    }

    private void DeviceInfoWindow_Closed(object? sender, EventArgs e)
    {
        EnableDevicesBox();
        deviceInfoWindow = null;
        AdbHelper.Instance.StopCommand();
        this.Activate();
    }

    /// <summary>
    /// Salva o conteúdo da caixa de status em um arquivo de texto
    /// </summary>
    private void Save_Click(object sender, RoutedEventArgs e)
    {
        string output = StatusText.Text;
        if (!string.IsNullOrEmpty(output))
        {
            string path = $"{appPath}\\Output";
            Directory.CreateDirectory(path);
            File.WriteAllText($"{path}\\StatusOutputText.txt", output);
            if (ShowMessage($"File Saved at: {path}\\StatusOutputText.txt\nDo you want to open it?", "File Saved", MessageBoxButton.YesNo, MessageBoxImage.Asterisk) == MessageBoxResult.Yes)
            {
                Process.Start(new ProcessStartInfo()
                {
                    FileName = $"{path}\\StatusOutputText.txt",
                    UseShellExecute = true,
                    Verb = "open"
                });
            }
        }
        else
        {
            ShowMessage("There is no output available", "Output empty");
        }
    }

    /// <summary>
    /// Atualiza o estado do botão de instalação quando um dispositivo é selecionado
    /// </summary>
    /// <param name="sender">O objeto que disparou o evento</param>
    /// <param name="e">Argumentos do evento de mudança de seleção</param>
    private void DevicesComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var selectedDevice = DevicesComboBox.SelectedItem as DeviceInfo;
        if (selectedDevice == null)
        {
            Install_Button.IsEnabled = false;
            return;
        }

        DisableEnable_SamsungDevices(selectedDevice);
        AllowInstall();
    }

    private void DisableEnable_SamsungDevices(DeviceInfo? selectedDevice)
    {
        string manufacter = selectedDevice?.Manufacturer ?? string.Empty;

        if (!manufacter.Equals("samsung", StringComparison.CurrentCultureIgnoreCase) || selectedDevice == null)
        {
            Kids_Button.IsEnabled = false;
            ParentalCare_Button.IsEnabled = false;
        }
        else
        {
            Kids_Button.IsEnabled = true;
            ParentalCare_Button.IsEnabled = true;
        }
    }
    /// <summary>
    /// Habilita a ComboBox de dispositivos
    /// </summary>
    public void EnableDevicesBox(bool enable = true)
    {
        List<DeviceInfo>? devices = DevicesComboBox.ItemsSource as List<DeviceInfo>;
        DevicesComboBox.IsEnabled = devices?.Count > 1 && enable;
    }
    #endregion
}
