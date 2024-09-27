using ApkInstaller.Helper_classes;
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

        private void StopScreenRecording()
        {
            AdbHelper.Instance.StopCommand();
        }

        private async void RealTimeScreen()
        {
            await AdbHelper.Instance.RealTimeScreen(_selectedDevice);
            Share_Button.Content = "Screen";
            Share_Button.Background = new SolidColorBrush(Color.FromRgb(247, 247, 247));
            ScreenRecordButton.IsEnabled = true;
        }

        private void EndRealTimeScreen()
        {
            AdbHelper.Instance.StopCommand();
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
                    logcatWindow = new LogcatWindow(_mainWindow, this, _selectedDevice, null);
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
    }
}
