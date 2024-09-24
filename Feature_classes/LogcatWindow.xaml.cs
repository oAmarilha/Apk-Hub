using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Media;
using ApkInstaller.Helper_classes;

namespace ApkInstaller;

public partial class LogcatWindow : Window, IComponentConnector
{
    private string _selectedDevice;

    private Process? _process;

    private string? _filter;

    private MainWindow _mainWindow;

    private Window _calledWindow;

    private string? _accountToken;

    private string? _deviceId;

    private string? _accountId;

    private string? _accountTokenUrl;

    private string? _clientId;

    public string appPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}\\ApkHub\\Log";


    public LogcatWindow(MainWindow mainWindow, Window calledWindow, string selectedDevice, string? filter)
    {
        InitializeComponent();
        _mainWindow = mainWindow;
        _selectedDevice = selectedDevice;
        _calledWindow = calledWindow;
        _filter = filter;
        Owner = _mainWindow;
        StartLogcat();
        _mainWindow.Hide();
        _calledWindow.Hide();
        base.Closing += Closing_Window;
    }

    private async void ClearLogcat()
    {
        await AdbHelper.Instance.RunAdbCommandAsync("logcat -c", _selectedDevice, shell: true, output => { });
    }

    private async void StartLogcat()
    {
        await AdbHelper.Instance.RunAdbCommandAsync("logcat -c", _selectedDevice, shell: true, output => { });
        StringBuilder logBuilder = new StringBuilder();
        DateTime lastUpdate = DateTime.Now;
        string logcatCommand = !string.IsNullOrEmpty(_filter) ? $"logcat *:I *:D *:W *:E *:V | grep \"{_filter}\"" : "logcat *:I *:D *:W *:E *:V";

        await AdbHelper.Instance.RunAdbCommandAsync(logcatCommand, _selectedDevice, shell: true, output =>
        {
            logBuilder.AppendLine(output);
            if ((DateTime.Now - lastUpdate).TotalMilliseconds > 100) // Atualiza a UI a cada 100ms
            {
                string logText = logBuilder.ToString();
                base.Dispatcher.BeginInvoke(new Action(() =>
                {
                    LogcatTextBox.AppendText(logText);
                    LogcatTextBox.ScrollToEnd();
                }));
                logBuilder.Clear();
                lastUpdate = DateTime.Now;
            }
        });
    }

    private void ExtractValuesFromLog(string logLine)
    {
        if (_accountToken == null)
        {
            Match match = Regex.Match(logLine, "Account-Token:\\s*(\\S+)");
            if (match.Success)
            {
                _accountToken = match.Groups[1].Value;
                base.Dispatcher.Invoke(() => AccountTokenText.Text = _accountToken);
            }
        }
        if (_deviceId == null)
        {
            Match match2 = Regex.Match(logLine, "Device-Id:\\s*(\\S+)");
            if (match2.Success)
            {
                _deviceId = match2.Groups[1].Value;
                base.Dispatcher.Invoke(() => DeviceIdText.Text = _deviceId);
            }
        }
        if (_accountId == null)
        {
            Match match3 = Regex.Match(logLine, "Account-Id:\\s*(\\S+)");
            if (match3.Success)
            {
                _accountId = match3.Groups[1].Value;
                base.Dispatcher.Invoke(() => AccountIdText.Text = _accountId);
            }
        }
        if (_accountTokenUrl == null)
        {
            Match match4 = Regex.Match(logLine, "Account-Token-Url:\\s*(\\S+)");
            if (match4.Success)
            {
                _accountTokenUrl = match4.Groups[1].Value;
                base.Dispatcher.Invoke(() => AccountTokenUrlText.Text = _accountTokenUrl);
            }
        }
        if (_clientId != null)
        {
            return;
        }
        Match match5 = Regex.Match(logLine, "Client-Id:\\s*(\\S+)");
        if (match5.Success)
        {
            _clientId = match5.Groups[1].Value;
            base.Dispatcher.Invoke(() => ClientIdText.Text = _clientId);
        }
    }

    private void StartStopButton_Click(object sender, RoutedEventArgs e)
    {
        if (StartStopButton.Content.ToString() == "Stop")
        {
            StopLogcat();
            return;
        }
        ClearLogcat();
        LogcatTextBox.Text = "";
        StartStopButton.Content = "Stop";
        StartStopButton.Background = Brushes.Red;
        StartLogcat();
    }

    private void StopLogcat()
    {
        AdbHelper.Instance.StopCommand();
        StartStopButton.Content = "Start";
        StartStopButton.Background = Brushes.Green;
    }

    private void SaveLog_Click(object sender, RoutedEventArgs e)
    {
        string value = DateTime.Now.ToString().Replace("/", "-").Replace(":", "-")
            .Replace(" ", "_");
        Directory.CreateDirectory($"{appPath}/Logcat/{_filter}/{value}/");
        try
        {
            string contents = LogcatTextBox.Text.ToString();
            string text = $"{appPath}\\Logcat\\{_filter}\\{value}";
            File.WriteAllText($"{text}\\{_filter}_{value}.txt", contents);
            if (_mainWindow.ShowMessage($"File Saved in: {text}\\{_filter}_{value}.txt\nDo you want to open it?", "File Saved", MessageBoxButton.YesNo, MessageBoxImage.Asterisk) == MessageBoxResult.Yes)
            {
                Process.Start(new ProcessStartInfo()
                {
                    FileName = text,
                    UseShellExecute = true,
                    Verb = "open"
                });
            }
        }
        catch (Exception ex)
        {
            _mainWindow.ShowMessage("Error saving the log file: " + ex.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Hand);
        }
    }

    public void Closing_Window(object? sender, CancelEventArgs e)
    {
        _mainWindow.Show();
        _calledWindow.Show();
    }
}
