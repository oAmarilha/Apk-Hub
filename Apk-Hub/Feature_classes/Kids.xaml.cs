using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using ApkInstaller.Helper_classes;

namespace ApkInstaller;

public partial class Kids : Window, IComponentConnector
{
    private MainWindow _mainWindow;

    private string _selectedDevice;

    private CancellationTokenSource _cancellationTokenSource;

    private AppWindow? _appWindow;

    public Automation? _automationWindow;

    private List<Window> childWindows = new List<Window>();

    private bool isClosing = false;
    public Kids(MainWindow mainWindow, string selectedDevice)
    {
        InitializeComponent();
        _mainWindow = mainWindow;
        _selectedDevice = selectedDevice;
        _cancellationTokenSource = new CancellationTokenSource();
        base.Owner = _mainWindow;
        base.Closing += ClosingWindow;
    }

    private T OpenChildWindow<T>(T childWindow) where T : Window
    {
        childWindows.Add(childWindow); // Adiciona a janela filha � lista
        childWindow.Closed += (s, e) => childWindows.Remove(childWindow); // Remove da lista quando fechada
        return childWindow;
    }

    private void ClosingWindow(object? sender, CancelEventArgs e)
    {
        // Cria uma lista tempor�ria para armazenar as janelas a serem fechadas
        var windowsToClose = new List<Window>(childWindows);

        // Itera sobre a lista tempor�ria
        foreach (var child in windowsToClose)
        {
            // Verifica se a janela est� aberta antes de cham�-la
            if (child.IsVisible)
            {
                child.Close(); // Fecha a janela filha
            }
        }
        _mainWindow.EnableDevicesBox();
        _cancellationTokenSource.Cancel();
        _mainWindow.ParentalCare_Button.IsEnabled = true;
        _mainWindow.Browse_Button.IsEnabled = true;
        _mainWindow.Activate();
    }

    private void OpenAppWindow(string actionType, bool shell)
    {
        if (_appWindow != null)
        {
            _appWindow.Closing -= AppWindow_Closing;
            _appWindow.Close();
            _appWindow = null;
        }
        _appWindow = OpenChildWindow(new AppWindow(this, _selectedDevice, _mainWindow, actionType, shell));
        _appWindow.Closing += AppWindow_Closing;
        _appWindow.Owner = this;
        _appWindow.Left = base.Left;
        _appWindow.Top = base.Top;
        _appWindow.Show();
        _mainWindow.Kids_Button.IsEnabled = false;
        Hide();
    }

    public void AppWindow_Closing(object? sender, CancelEventArgs e)
    {
        _mainWindow.Kids_Button.IsEnabled = true;
        Show();
    }

    private void ClearPkg_Button(object sender, RoutedEventArgs e)
    {
        OpenAppWindow("pm clear", shell: true);
    }

    private void Uninstall_Button(object sender, RoutedEventArgs e)
    {
        OpenAppWindow("uninstall", shell: false);
    }

    private async void LogcatClear_Button(object sender, RoutedEventArgs e)
    {
        string logcat = "";
        try
        {
            await AdbHelper.Instance.RunAdbCommandAsync("logcat -c", output =>
            {
                logcat += output;
            }, _selectedDevice, shell: true);

            if (string.IsNullOrEmpty(logcat))
            {
                _mainWindow.UpdateStatusText("Logcat cleared succesfully", isSuccess: true, clear: true);
            }
            else
            {
                _mainWindow.UpdateStatusText("Logcat not clared", isError: true);
            }
        }
        catch (Exception ex4)
        {
            Exception ex3 = ex4;
            Exception ex = ex3;
            base.Dispatcher.Invoke(delegate
            {
                _mainWindow.UpdateStatusText($"Error: {ex.Message}", isError: true);
            });
        }
    }

    private void LogcatView_Button(object sender, RoutedEventArgs e)
    {
        OpenAppWindow("logcat", shell: false);
    }

    private void Automation_Click(object sender, RoutedEventArgs e)
    {
        if (_automationWindow == null)
        {
            _automationWindow = OpenChildWindow(new Automation(_selectedDevice));
            _automationWindow.Show();
            Hide();
            _mainWindow.Hide();
            _automationWindow.Closing += _automationWindow_Closing;
        }
    }

    private async void _automationWindow_Closing(object? sender, CancelEventArgs e)
    {
        if (isClosing) return;
        isClosing = true;
        await _automationWindow!.StopPythonExecution();
        isClosing = false;
        _automationWindow = null;
        Show();
        _mainWindow.Show();
    }

    private async void Open_Kids_Click(object sender, RoutedEventArgs e)
    {
        bool user = FindStr("user", await AdbHelper.Instance.GetAdbReturn("getprop ro.build.type", _selectedDevice, true));
        _mainWindow.UpdateStatusText("Trying to open Kids Home app", clear:true);
        string output = await AdbHelper.Instance.GetAdbReturn("am start -n com.sec.android.app.kidshome/com.sec.android.app.kidshome.apps.ui.AppsActivity",
            _selectedDevice, true);
        if (FindStr("Starting: Intent", output) || FindStr("intent has been delivered to currently running", output))
        {
            Application.Current.Dispatcher.Invoke(() =>
            _mainWindow.UpdateStatusText("Kids Home has been opened.", isSuccess: true, clear: true));
            return;
        }
        else if (FindStr("does not exist.", output) && !user)
        {
            string openKidsSetup = await AdbHelper.Instance.GetAdbReturn("am start -n com.sec.android.app.kidshome/com.sec.android.app.kidshome.parentalcontrol.setupwizard.ui.SetupWizardIntroActivity", _selectedDevice, true);
            if (FindStr("Starting: Intent", openKidsSetup) || FindStr("intent has been delivered to currently running", openKidsSetup))
            {
                Application.Current.Dispatcher.Invoke(() =>
                _mainWindow.UpdateStatusText("Kids Home is not configured. The configuration has been started.\nCheck the device again.", isWarning: true, clear: true));
                return;
            }
        }
        if (user)
        {
            string downloaded = await AdbHelper.Instance.GetAdbReturn("am start -n com.samsung.android.kidsinstaller/com.samsung.android.kidsinstaller.install.ui.KidsHomeInstallActivity", _selectedDevice, true);
            if (FindStr("Starting: Intent", downloaded) || FindStr("intent has been delivered to currently running", downloaded))
                _mainWindow.UpdateStatusText("Kids Home is not downloaded. Check the device to download it.", isWarning: true, clear: true);
            return;
        }
        _mainWindow.UpdateStatusText("Kids Home was not opened.\nCheck if it is correctly installed and configured.", isError: true, clear: true);
    }

    private static bool FindStr(string find, string str)
    {
        bool success = str.Contains(find);
        if (success) return true;
        else return false;
    }
}