using ApkInstaller.Helper_classes;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Media;

namespace ApkInstaller;

public partial class LogcatWindow : Window, IComponentConnector
{
    private string _selectedDevice;

    private string? _filter;

    private MainWindow _mainWindow;

    private Window _calledWindow;

    private string? _accountToken;

    private string? _deviceId;

    private string? _accountId;

    private string? _accountTokenUrl;

    private string? _clientId;

    public string appPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}\\ApkHub\\Log";

    StringBuilder? logBuilder;

    CancellationTokenSource? cancellationTokenSource;

    public LogcatWindow(MainWindow mainWindow, Window calledWindow, string selectedDevice, string? filter)
    {
        InitializeComponent();
        _mainWindow = mainWindow;
        _selectedDevice = selectedDevice;
        _calledWindow = calledWindow;
        _filter = filter;
        Owner = _mainWindow;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        StartLogcat();
        _mainWindow.Hide();
        _calledWindow.Hide();
        base.Closing += Closing_Window;
    }

    private async void ClearLogcat()
    {
        await AdbHelper.Instance.RunAdbCommandAsync("logcat -c", output => { }, _selectedDevice, shell: true);
    }

    private async void StartLogcat()
    {
        cancellationTokenSource = new CancellationTokenSource();
        string? appPID = null;
        if (_filter != null)
        {
            appPID = await AdbHelper.Instance.GetAdbReturn($"pidof -s '{_filter}'", _selectedDevice, true);
            if (string.IsNullOrEmpty(appPID))
            {
                MessageBox.Show("Não foi possível encontrar o PID do aplicativo, verifique se o App está aberto.");
                Closing_Window(null, new CancelEventArgs());
                Close();
                return;
            }

            // Start a background task to monitor PID
            _ = Task.Run(async () =>
            {
                while (!cancellationTokenSource.Token.IsCancellationRequested)
                {
                    await Task.Delay(1000); // Check every second
                    string currentAppPID = await AdbHelper.Instance.GetAdbReturn($"pidof -s '{_filter}'", _selectedDevice, true);

                    if (currentAppPID != appPID)
                    {
                        // Restart logcat if PID changed
                        appPID = currentAppPID;
                        // Trigger UI to restart logcat
                        if (!string.IsNullOrEmpty(appPID))
                        {
                            base.Dispatcher.Invoke(() => RestartLogcat(appPID));
                        }
                    }
                }
            }, cancellationTokenSource.Token);
        }

        // Clear previous logs
        await AdbHelper.Instance.RunAdbCommandAsync("logcat -c", output => { }, _selectedDevice, shell: true);
        
        logBuilder = new StringBuilder();
        DateTime lastUpdate = DateTime.Now;

        // Comprehensive logcat command to capture full logs
        string logcatCommand = string.IsNullOrEmpty(_filter) 
            ? "logcat -v threadtime *:V" 
            : $"logcat -v threadtime --pid={appPID} *:V";

        await AdbHelper.Instance.RunAdbCommandAsync(logcatCommand, output =>
        {
            if (output == null) return;

            // Special handling for specific filter
            if (_filter == "com.samsung.android.app.parentalcare"){
                ExtractValuesFromLog(output);
            }
            
            // Append each log line immediately
            lock (logBuilder)
            {
                logBuilder.AppendLine(output);
            }

            // Update UI periodically or on significant events
            if ((DateTime.Now - lastUpdate).TotalMilliseconds > 100 || 
                output.Contains("FATAL EXCEPTION") || 
                output.Contains("AndroidRuntime") || 
                output.Contains("E/"))
            {
                base.Dispatcher.Invoke(() =>
                {
                    lock (logBuilder)
                    {
                        string logText = logBuilder.ToString();
                        LogcatTextBox.AppendText(logText);
                        LogcatTextBox.ScrollToEnd();
                        logBuilder.Clear();
                    }
                    lastUpdate = DateTime.Now;
                });
            }
        }, _selectedDevice, shell: true, continueOnError: true);
    }

    private async void RestartLogcat(string newPID)
    {
        // Stop current logcat and restart with new PID
        StopLogcat(false);
        await Task.Delay(100); // Small delay to ensure previous process stops
        StartLogcat();
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

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        // Reset the stored values so they can be extracted again
        _accountToken = null;
        Application.Current.Dispatcher.Invoke(() => AccountTokenText.Text = "null");
        await AdbHelper.Instance.RunAdbCommandAsync("logcat -c", output => { }, _selectedDevice, shell: true);

        await AdbHelper.Instance.RunAdbCommandAsync($"am start -n com.osp.app.signin/com.samsung.android.samsungaccount.setting.ui.family.main.FamilyGroupMainActivity", output => { }, _selectedDevice, shell: true);

        // Get current log content and process it
        if (logBuilder != null)
        {
            ExtractValuesFromLog(logBuilder.ToString());
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

    private void StopLogcat(bool stopButton = true)
    {
        cancellationTokenSource?.Cancel();
        AdbHelper.Instance.StopCommand();
        if (stopButton)
        {
            StartStopButton.Content = "Start";
            StartStopButton.Background = Brushes.Green;
        }
    }

    private void SaveLog_Click(object sender, RoutedEventArgs e)
    {
        string value = DateTime.Now.ToString().Replace("/", "-").Replace(":", "-")
            .Replace(" ", "_");
        string namePath = _filter ?? "FullLogcat";
        Directory.CreateDirectory($"{appPath}/Logcat/{namePath}/{value}/");
        try
        {
            string contents = LogcatTextBox.Text.ToString();
            string text = $"{appPath}\\Logcat\\{namePath}\\{value}";
            File.WriteAllText($"{text}\\{namePath}_{value}.txt", contents);
            if (_mainWindow.ShowMessage($"File Saved at: {text}\\{namePath}_{value}.txt\nDo you want to open it?", "File Saved", MessageBoxButton.YesNo, MessageBoxImage.Asterisk) == MessageBoxResult.Yes)
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
        StopLogcat(false);
        _mainWindow.Show();
    }
}
