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

public partial class MainWindow : MetroWindow, IComponentConnector
{
    private Settings? settingsWindow;

    private Kids? kidsWindow;

    private UsbDeviceNotifier? usbDeviceNotifier;

    private More? moreWindow;

    private bool loopCancelation = false;

    private bool success;

    public string appPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}\\ApkHub\\Log";
    public static MainWindow Instance { get; private set; } = null!;

    private List<Window> childWindows = new List<Window>();

    public MainWindow()
    {
        InitializeComponent();
        this.WindowTitleBrush = new SolidColorBrush(Color.FromRgb(75, 10, 198));
        this.BorderBrush = new SolidColorBrush(Color.FromRgb(75, 10, 198));
        base.Loaded += MainWindow_Loaded;
        Instance = this;
        Install_Button.Content = "Install APKs";
        base.Closing += MainWindow_Closing;

    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        PopulateDevices();
        Directory.CreateDirectory(appPath);
        usbDeviceNotifier = new UsbDeviceNotifier(this);
        usbDeviceNotifier.UsbDeviceChanged += OnUsbDeviceChanged!;
    }

    private async void OnUsbDeviceChanged(object sender, EventArgs e)
    {
        await Task.Delay(1000);
        PopulateDevices();
    }

    public async void PopulateDevices()
    {
        DevicesComboBox.ItemsSource = null;
        Install_Button.IsEnabled = false;
        UpdateStatusText("Checking devices connected...", clear: true);

        var connectedDevices = await Task.Run(GetConnectedDevices);
        var deviceList = connectedDevices.Select(d => $"{d.Value} ({d.Key})").ToList();

        Dispatcher.Invoke(() =>
        {
            DevicesComboBox.ItemsSource = deviceList;
            DevicesComboBox.SelectedItem = deviceList.Count == 1 ? deviceList[0] : null;

            int count = deviceList.Count;
            UpdateStatusText(count > 0 ? $"{count} Device(s) Connected" : "No Device Connected", count == 0, count > 0, true);

            Install_Button.IsEnabled = ApkFilesList.Items.Count > 0 && count > 0;
            DevicesComboBox.IsEnabled = count > 1;

            if (DevicesComboBox.SelectedItem == null)
            {
                foreach (var window in Application.Current.Windows.OfType<Window>().Where(w => w != Application.Current.MainWindow))
                {
                    window.Close();
                }
            }
        });
    }

    private T OpenChildWindow<T>(T childWindow) where T : Window
    {
        childWindows.Add(childWindow); // Adiciona a janela filha à lista
        childWindow.Closed += (s, e) => childWindows.Remove(childWindow); // Remove da lista quando fechada
        return childWindow;
    }

    private string? CheckDeviceComboBox()
    {
        return DevicesComboBox.SelectedItem?.ToString();
    }

    private void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        PopulateDevices();
    }

    private void ChangeButtonVisibility(bool change)
    {
        foreach (var child in MainWindowGrid.Children.OfType<StackPanel>())
        {
            foreach (var stackChild in child.Children.OfType<Button>())
            {
                stackChild.IsEnabled = change;
            }
        }

        Install_Button.IsEnabled = DevicesComboBox.Items.Count > 0 && change && ApkFilesList.Items.Count > 0;
    }

    public MessageBoxResult ShowMessage(string message, string? title = null, MessageBoxButton button = MessageBoxButton.OK, MessageBoxImage icon = MessageBoxImage.Warning)
    {
        return MessageBox.Show(message, title, button, icon);
    }

    private void CheckDuplicatedFiles(List<string> errorMessages, List<string> addedFiles)
    {
        if (errorMessages.Any())
        {
            string errorMessage = errorMessages.Count() == 1
                ? $"The following file:\n'{string.Join("', '", errorMessages)}' is already selected, please remove it and try again."
                : $"The following files:\n'{string.Join("', '", errorMessages)}' are already selected, please remove them and try again.";

            if (addedFiles.Any())
            {
                string addedMessage = addedFiles.Count() == 1
                    ? $"Additionally, the following file was successfully added:\n'{string.Join("', '", addedFiles)}'"
                    : $"Additionally, the following files were successfully added:\n'{string.Join("', '", addedFiles)}'";

                errorMessage += $"\n\n{addedMessage}";
            }

            UpdateStatusText("Duplicated file(s)", clear: true, isError: true);
            ShowMessage(errorMessage, "File Selection Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            ChangeButtonVisibility(true);
            return;
        }
        UpdateStatusText("Files loaded", clear: true);
    }

    private void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        ChangeButtonVisibility(false);
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

            foreach (var filename in openFileDialog.FileNames)
            {
                string error = AddApkFile(filename);
                string fileNameOnly = System.IO.Path.GetFileName(filename); // Extrai apenas o nome do arquivo

                if (!string.IsNullOrEmpty(error))
                {
                    errorMessages.Add(fileNameOnly);
                }
                else
                {
                    addedFiles.Add(fileNameOnly); // Adiciona apenas o nome do arquivo
                }
            }
            CheckDuplicatedFiles(errorMessages, addedFiles);
        }
        ChangeButtonVisibility(true);
    }

    private void Grid_DragOver(object sender, DragEventArgs e)
    {
        // Verifica se os dados do arraste são arquivos
        if (e.Data.GetData(DataFormats.FileDrop, autoConvert: false) is string[] files)
        {
            // Verifica se algum arquivo .apk está presente
            bool hasApkFile = files.Any(file => file.EndsWith(".apk"));

            // Atualiza AllowDrop e o efeito de arraste
            ApkFilesList.AllowDrop = true; // Permite o drop independentemente
            e.Effects = hasApkFile ? DragDropEffects.Copy : DragDropEffects.None;
        }
        e.Handled = true; // Marca o evento como tratado
    }

    private void Grid_Drop(object sender, DragEventArgs e)
    {
        var errorMessages = new List<string>();
        var addedFiles = new List<string>();

        if (e.Data.GetData(DataFormats.FileDrop, autoConvert: false) is string[] files)
        {
            foreach (var filename in files)
            {
                if (filename.EndsWith(".apk"))
                {
                    string error = AddApkFile(filename);
                    string fileNameOnly = System.IO.Path.GetFileName(filename);

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
            CheckDuplicatedFiles(errorMessages, addedFiles);
        }
    }

    private string AddApkFile(string filename)
    {
        string appError = "";
        string fileName = Path.GetFileName(filename);
        StackPanel stackPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal
        };

        foreach (StackPanel item in ApkFilesList.Items)
        {
            foreach (var child in item.Children)
            {
                if (child is TextBlock itemText)
                {
                    string text = itemText.Text;
                    if (Path.GetFileName(filename) == itemText.Text)
                    {
                        appError = (Path.GetFileName(filename));
                        return appError;
                    }
                }
            }
        }
        ChangeButtonVisibility(true);

        TextBlock element = new TextBlock
        {
            Text = fileName,
            Margin = new Thickness(0.0, 0.0, 10.0, 0.0)
        };
        Button button = new Button
        {
            Content = "Delete",
            Tag = filename
        };
        button.Click += DeleteButton_Click;
        stackPanel.Children.Add(element);
        stackPanel.Children.Add(button);
        stackPanel.Tag = filename;
        ApkFilesList.Items.Add(stackPanel);
        return appError;
    }

    private void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Parent is StackPanel stackPanel)
        {
            ApkFilesList.Items.Remove(stackPanel);

            // Habilita ou desabilita o botão de instalação com base na contagem de itens
            Install_Button.IsEnabled = ApkFilesList.Items.Count > 0;
        }
    }

    private async void InstallButton_Click(object sender, RoutedEventArgs e)
    {
        string? device = CheckDeviceComboBox();
        if (device == null)
        {
            UpdateStatusText("Please select a device.", isError: true, clear: true);
        }
        else if (Install_Button.Content.ToString() == "Install APKs")
        {
            ApkFilesList.IsEnabled = false;
            Install_Button.Content = "Stop";
            Install_Button.Background = Brushes.Red;
            List<string> list = new List<string>();
            foreach (StackPanel item2 in (IEnumerable)ApkFilesList.Items)
            {
                string item = item2.Tag.ToString();
                list.Add(item);
            }
            string deviceSerialByName = GetDeviceSerialByName(device);
            await InstallApks(deviceSerialByName, list);
        }
        else
        {
            loopCancelation = true;
            success = false;
            AdbHelper.Instance.StopCommand();
            UpdateStatusText("Installation canceled", isError: true);
            ApkFilesList.IsEnabled = true;
            Install_Button.Content = "Install APKs";
            Install_Button.Background = Brushes.Green;
            return;
        }
    }

    private async Task InstallApks(string device, List<string> apkFiles)
    {
        string outputResult = "";
        Dispatcher.Invoke(() =>
        {
            UpdateStatusText("Initializing the installation", clear: true);
        });
        try
        {
            success = true;
            foreach (string apkFile in apkFiles)
            {
                if (loopCancelation == false)
                {
                    UpdateStatusText($"\nInstalling \"{apkFile}\"");
                    await AdbHelper.Instance.RunAdbCommandAsync($"install -r -d \"{apkFile}\"", output =>
                    {
                        base.Dispatcher.Invoke(() =>
                        {
                            UpdateStatusText(output);
                            outputResult += output;
                        });
                    }, device, shell: false);

                    if (!outputResult.Contains("Success"))
                    {
                        loopCancelation = true;
                        success = false;
                    }
                }
                else
                {
                    success = false;
                    break;
                }
            }
            Dispatcher.Invoke(() =>
            {
                if (success)
                {
                    UpdateStatusText("Installation complete", isSuccess: true);
                }
                else
                {
                    UpdateStatusText("Installation not complete.", isError: true);
                }
            });
            Install_Button.Content = "Install APKs";
            Install_Button.Background = Brushes.Green;
            ApkFilesList.IsEnabled = true;
            loopCancelation = false;
        }
        catch (Exception ex)
        {
            Dispatcher.Invoke(() =>
            {
                UpdateStatusText($"Error: {ex.Message}", isError: true);
            });
        }
    }


    private async Task<Dictionary<string, string>> GetConnectedDevices()
    {
        string? devices = null;
        Dictionary<string, string> dictionary = new Dictionary<string, string>();
        await AdbHelper.Instance.RunAdbCommandAsync("devices", output => { devices += $"\n{output}"; }, generalCommand: true);
        using StringReader stringReader = new StringReader(devices);
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

    public async Task<string> GetDeviceName(string serial)
    {
        string name = "";
        await AdbHelper.Instance.RunAdbCommandAsync("getprop ro.product.model", output =>
        {
            name += output;
        }, serial, true);
        return name.Split('\n')[0].Trim();
    }

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
            ShowWindow(kidsWindow, 250, moreWindow, settingsWindow);
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

    private void KidsWindow_Closed(object? sender, EventArgs e)
    {
        if (ApkFilesList.Items.Count > 0)
        {
            HandleWindowClosed(Install_Button);
        }
        AdbHelper.Instance.StopCommand();
        kidsWindow = null;
    }


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
            ShowWindow(settingsWindow, 220, moreWindow, kidsWindow);
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

    private void PcWindow_Closed(object? sender, EventArgs e)
    {
        if (ApkFilesList.Items.Count > 0)
        {
            HandleWindowClosed(ApkFilesList, Install_Button);
        }
        AdbHelper.Instance.StopCommand();
        settingsWindow = null;
    }


    public void ActivateDevicesBox()
    {
        if (DevicesComboBox.Items.Count > 1)
        {
            DevicesComboBox.IsEnabled = true;
        }
    }

    public void UpdateStatusText(string? message = null, bool isError = false, bool isSuccess = false, bool clear = false)
    {
        base.Dispatcher.Invoke(delegate
        {
            StatusText.Foreground = (isError ? Brushes.Red : (isSuccess ? Brushes.Green : Brushes.White));
            if (clear == true)
            {
                StatusText.Text = string.Empty;
            }

            if (!string.IsNullOrEmpty(message))
            {
                //Add time in terminal log?
                //string currentDateTime = DateTime.Now.ToString("[MM-dd HH:mm:ss.fff]");
                StatusText.Text += /* $"{currentDateTime}\n" + */ message + Environment.NewLine;
            }

            StatusText.ScrollToEnd();
        });
    }

    public void EmptyOutput_Button(object sender, RoutedEventArgs e)
    {
        ApkFilesList.Items.Clear();
        UpdateStatusText(clear: true);
    }

    private void More_Button_Click(object sender, RoutedEventArgs e)
    {
        string? device = CheckDeviceComboBox();
        if (device == null)
        {
            ShowMessage("Please select a device before opening more options.", "No Device Selected", MessageBoxButton.OK, MessageBoxImage.Exclamation);
        }
        else if (moreWindow == null)
        {
            moreWindow = OpenChildWindow(new More(this, GetDeviceSerialByName(device), settingsWindow, kidsWindow));
            ShowWindow(moreWindow, 250, settingsWindow, kidsWindow);
            DevicesComboBox.IsEnabled = false;
            moreWindow.Closed += MoreWindow_Closed;
        }
        else
        {
            SystemSounds.Exclamation.Play();
            moreWindow.Activate();
        }
    }

    private void MoreWindow_Closed(object? sender, EventArgs e)
    {
        ActivateDevicesBox();
        moreWindow = null;
        AdbHelper.Instance.StopCommand();
        this.Activate();
    }

    private void ShowWindow(Window window, double extraWidth, Window? otherWindow1 = null, Window? otherWindow2 = null)
    {
        if (base.Left + base.Width + extraWidth >= SystemParameters.PrimaryScreenWidth || otherWindow1 != null || otherWindow2 != null)
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

    private void HandleWindowClosed(params Control[] controlsToEnable)
    {
        foreach (var control in controlsToEnable)
        {
            control.IsEnabled = true;
        }
        this.Activate();
    }

    private async void MainWindow_Closing(object? sender, CancelEventArgs e)
    {
#if !DEBUG
        await AdbHelper.Instance.RunAdbCommandAsync("kill-server", output => { }, generalCommand: true);
#endif
        // Cria uma lista temporária para armazenar as janelas a serem fechadas
        var windowsToClose = new List<Window>(childWindows);

        // Itera sobre a lista temporária
        foreach (var child in windowsToClose)
        {
            // Verifica se a janela está aberta antes de chamá-la
            if (child.IsVisible)
            {
                child.Close(); // Fecha a janela filha
            }
        }
        Application.Current.Shutdown();
    }

    /// <summary>
    /// Enable or disable buttons.
    /// </summary>
    /// <param name="nameButton">Button name that it will be changed.</param>
    /// <param name="status">New button state true - activated, false - deactivated.</param>
    private void Button_Status(List<Button> nameButton, List<bool> status)
    {
        foreach (var button in nameButton)
        {
            foreach (var action in status)
            {
                button.IsEnabled = action;
            }
        }
    }

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
}
