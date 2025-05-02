using ApkInstaller.Feature_classes;
using ApkInstaller.Helper_classes;
using System.Diagnostics;
using System.IO;
using System.Media;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;

namespace ApkInstaller
{
    /// <summary>
    /// Interaction logic for More.xaml
    /// </summary>
    public partial class More : Window, IComponentConnector
    {
        private MainWindow _mainWindow;

        private LogcatWindow? logcatWindow;

        private PkgAction? pkgActionWindow;

        private Settings? _pcWindow;

        private Kids? _kidsWindow;

        private string _selectedDevice;

        private string localFile = $"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}\\ApkHub\\Log";
        public More(MainWindow mainWindow, string selectedDevice, Settings? pcWindow, Kids? kidsWindow)
        {
            InitializeComponent();
            _mainWindow = mainWindow;
            _selectedDevice = selectedDevice;
            _pcWindow = pcWindow;
            _kidsWindow = kidsWindow;
            Owner = _mainWindow;
        }

        private async void ScreenRecordButton_Click(object sender, RoutedEventArgs e)
        {
            if (ScreenRecordButton.Content.ToString() == "Record")
            {
                RealTimeScreen();
                Share_Button.IsEnabled = false;
                ScreenRecordButton.Content = "Stop";
                ScreenRecordButton.Background = Brushes.Red;
                await AdbHelper.Instance.StartScreenRecording(_selectedDevice, localFile);
            }
            else
            {
                Share_Button.IsEnabled = true;
                ScreenRecordButton.Content = "Record";
                ScreenRecordButton.Background = Brushes.Green;
                StopScreenRecording();
            }
        }

        static void StopScreenRecording()
        {
            AdbHelper.Instance.StopCommand();
        }

        private async void RealTimeScreen()
        {
            await AdbHelper.ScreenShareInstance.RealTimeScreen(_selectedDevice);
            Share_Button.Content = "Screen";
            Share_Button.Background = new SolidColorBrush(Color.FromRgb(247, 247, 247));
            ScreenRecordButton.IsEnabled = true;
        }

        private static void EndRealTimeScreen()
        {
            AdbHelper.ScreenShareInstance.EndRealTimeScreen();
        }

        private void RemoteScreen_Click(object sender, RoutedEventArgs e)
        {
            if (Share_Button.Content.ToString() == "Screen")
            {
                ScreenRecordButton.IsEnabled = false;
                Share_Button.Content = "Stop";
                Share_Button.Background = Brushes.Red;
                RealTimeScreen();
            }
            else
            {
                ScreenRecordButton.IsEnabled = true;
                Share_Button.Content = "Screen";
                EndRealTimeScreen();
            }
        }

        private void UninstallApp_Button_Click(object sender, RoutedEventArgs e)
        {

            if ((pkgActionWindow == null || !pkgActionWindow.IsVisible) && _mainWindow.DevicesComboBox.SelectedItem != null)
            {
                pkgActionWindow = new PkgAction(_mainWindow, this, _selectedDevice);
                pkgActionWindow.Title = "Uninstall";
                pkgActionWindow.Send_Command.Content = "Uninstall";
                pkgActionWindow.TextTitle.Text = "Uninstall App:";
                pkgActionWindow.Show();
            }
        }

        private void Logcat_Button_Click(object sender, RoutedEventArgs e)
        {
            if (_mainWindow.ShowMessage("Do you want to get the full device logcat?\nPress 'No' to choose a package name to get.", "Full device log will be catch", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            {
                if (pkgActionWindow == null || !pkgActionWindow.IsVisible)
                {
                    pkgActionWindow = new PkgAction(_mainWindow, this, _selectedDevice);
                    pkgActionWindow.Title = "Logcat pkg";
                    pkgActionWindow.TextTitle.Text = "Get logcat:";
                    pkgActionWindow.Send_Command.Content = "Logcat";
                    pkgActionWindow.Show();
                }
            }
            else
            {
                if (logcatWindow == null)
                {
                    logcatWindow = new LogcatWindow(_mainWindow, _mainWindow.moreWindow, _selectedDevice, null);
                    if (_mainWindow.Top + _mainWindow.Height + 450.0 >= SystemParameters.PrimaryScreenHeight)
                    {
                        logcatWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    }
                    else
                    {
                        logcatWindow.Top = _mainWindow.Top + _mainWindow.Height;
                        logcatWindow.Left = _mainWindow.Left;
                    }
                    Grid.SetRow(logcatWindow.Buttons_StackPanel, 0);
                    logcatWindow.StartStopButton.Margin = new Thickness(0, 0, 0, 5);
                    logcatWindow.Buttons_StackPanel.Orientation = Orientation.Vertical;
                    logcatWindow.logcatGrid.Children.Remove(logcatWindow.PC_Info);
                    logcatWindow.Show();
                    logcatWindow.Closing += LogcatWindow_Closing;
                }
            }
        }

        private void LogcatWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            logcatWindow = null;
            if (Application.Current.Windows.OfType<More>().Any()) this.Show();
        }

        private void ClearAPK_Button_Click(object sender, RoutedEventArgs e)
        {
            if ((pkgActionWindow == null || !pkgActionWindow.IsVisible) && _mainWindow.DevicesComboBox.SelectedItem != null)
            {
                pkgActionWindow = new PkgAction(_mainWindow, this, _selectedDevice);
                pkgActionWindow.Title = "Clear app";
                pkgActionWindow.TextTitle.Text = "Clear package:";
                pkgActionWindow.Send_Command.Content = "ClearPkg";
                pkgActionWindow.Show();
            }
        }

        private void Run_Adb_Click(object sender, RoutedEventArgs e)
        {
            if ((pkgActionWindow == null || !pkgActionWindow.IsVisible) && _mainWindow.DevicesComboBox.SelectedItem != null)
            {
                pkgActionWindow = new PkgAction(_mainWindow, this, _selectedDevice);
                pkgActionWindow.Title = "Run your adb command";
                pkgActionWindow.TextTitle.Text = "Adb command:";
                pkgActionWindow.Send_Command.Content = "Run";
                pkgActionWindow.Show();
            }
        }

        private async void GetCurrentApp_Button_Click(object sender, RoutedEventArgs e)
        {
            List<string> currentApp = await GetCurrentApp();
            if (currentApp.Count > 0) base.Dispatcher.Invoke(() => _mainWindow.UpdateStatusText($"Package: {currentApp[0]}\nActivity: {currentApp[1]}", clear: true));
            else base.Dispatcher.Invoke(() => _mainWindow.UpdateStatusText($"App package and activity not found", clear: true));
        }

        private async Task<List<string>> GetCurrentApp()
        {
            List<string> list = [];
            _mainWindow.UpdateStatusText(clear: true);
            string result = await AdbHelper.Instance.GetAdbReturn("dumpsys window", _selectedDevice, true);
            var currentFocusLine = result.Split('\n')
                .FirstOrDefault(line => line.Contains("mCurrentFocus"));
            if (currentFocusLine != null)
            {
                var match = Regex.Match(currentFocusLine, @"([a-zA-Z0-9\.]+)/([a-zA-Z0-9\.]+)");
                if (match.Success)
                {
                    string package = match.Groups[1].Value;
                    string activity = match.Groups[2].Value;
                    list.Add(package);
                    list.Add(activity);
                }
            }
            return list;
        }

        private void GetFile_Button_Click(object sender, RoutedEventArgs e)
        {
            var fileExplorer = new FileExplorerWindow(_selectedDevice);
            fileExplorer.Owner = _mainWindow;
            fileExplorer.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            _mainWindow.Hide();
            this.Hide();
            fileExplorer.Closing += FileExplorer_Closing;
            fileExplorer.ShowDialog();
        }

        private void FileExplorer_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            _mainWindow.Show();
            if (Application.Current.Windows.OfType<More>().Any()) this.Show();
        }

        private async void Screenshot_Button_Click(object sender, RoutedEventArgs e)
        {
            List<string> package = await GetCurrentApp();
            string dirPath = string.Empty;
            string screenshotsPath = $"{localFile}\\Screenshots";
            string time = DateTime.Now.ToString().Replace("/", "-").Replace(":", "-")
            .Replace(" ", "_");
            string deviceName = await _mainWindow.GetDeviceName(_selectedDevice);
            if (package.Count > 0) dirPath = Directory.CreateDirectory($"{screenshotsPath}\\{deviceName}\\{package[0]}").ToString();
            else dirPath = Directory.CreateDirectory($"{screenshotsPath}\\{deviceName}\\AppPackageNotFound").ToString();
            string screenshotPath = await AdbHelper.Instance.GetAdbReturn("ls /sdcard/apk_hub", _selectedDevice, true);
            if(screenshotPath.Contains("No such file or directory")) await AdbHelper.Instance.RunAdbCommandAsync("mkdir /sdcard/apk_hub", output => { }, _selectedDevice, true);
            await AdbHelper.Instance.RunAdbCommandAsync("screencap /sdcard/apk_hub/apkhub_screenshot.png", output => { }, _selectedDevice, true) ;
            string result = await AdbHelper.Instance.GetAdbReturn($"pull /sdcard/apk_hub/apkhub_screenshot.png {dirPath}\\{package[0]}_{time}.png", _selectedDevice);
            if (result.Contains("1 file pulled"))
            {
                Dispatcher.Invoke(() => _mainWindow.UpdateStatusText("Screenshot taken", isSuccess: true, clear: true));
                SystemSounds.Exclamation.Play();
                if (_mainWindow.ShowMessage("The screenshot was taken, do you want to open it?", "Success", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    Process.Start(new ProcessStartInfo()
                    {
                        FileName = dirPath,
                        UseShellExecute = true,
                        Verb = "open"
                    });
                }

            }
            else Dispatcher.Invoke(() => _mainWindow.UpdateStatusText($"There is an error while the screenshot was being taken: {result}", isError: true, clear: true));
        }

        private async void RebootTurnoff_Button_Click(object sender, RoutedEventArgs e)
        {
            Dictionary <string, List<string>> action = new()
            {
                {"Turnoff", ["turned off", " -p", "turn the device off"] },
                {"Reboot", ["restarted", "", "restart the device"] },
                {"DOWNLOAD MODE", ["restarted to download mode", " download", "restart in download mode"] }
            };
            var content = (sender as Button)!.Content;
            string command = content is TextBlock ? (content as TextBlock)!.Text : content.ToString()!;
            _mainWindow.UpdateStatusText($"Sending command to {action[command][2]}, check your device.", isWarning: true, clear: true);
            string resultcommand = await AdbHelper.Instance.GetAdbReturn($"reboot{action[command][1]}", _selectedDevice, true);
            bool result = string.IsNullOrEmpty(resultcommand) || resultcommand.Contains("Done");
            await _mainWindow.DisconnectIpDevices();
            if (result)
            {
                _mainWindow.UpdateStatusText($"The device has been {action[command][0]}", isSuccess: true, clear: true);
            }
            else
            {
                _mainWindow.UpdateStatusText($"The device has not been {action[command][0]}, check it and try again.", isError: true, clear: true);
            }
        }
    }
}
