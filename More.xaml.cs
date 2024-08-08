using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Media;
using System.Security.AccessControl;
using System.Threading;
using System.Threading.Tasks;
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

        private string _selectedDevice;

        private string localFile = Directory.GetCurrentDirectory() + "\\log";
        public More(MainWindow mainWindow, string selectedDevice)
        {
            InitializeComponent();
            _mainWindow = mainWindow;
            _selectedDevice = selectedDevice;
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
                await AdbHelper.Instance.StartScreenRecording(_mainWindow, _selectedDevice, localFile);
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
            await AdbHelper.Instance.RunAdbCommandAsync("", _selectedDevice, false, output =>
            {
                _mainWindow.UpdateStatusText(output);
            }, "scrcpy");
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
                pkgActionWindow = new PkgAction(_mainWindow, this,_selectedDevice);
                pkgActionWindow.Title = "Uninstall";
                pkgActionWindow.Send_Command.Content = "Uninstall";
                pkgActionWindow.TextTitle.Text = "Uninstall App:";
                pkgActionWindow.Show();
            }
        }

        private void Logcat_Button_Click(object sender, RoutedEventArgs e)
        {
            if ((pkgActionWindow == null || !pkgActionWindow.IsVisible) && _mainWindow.DevicesComboBox.SelectedItem != null)
            {
                pkgActionWindow = new PkgAction(_mainWindow, this, _selectedDevice);
                pkgActionWindow.Title = "Logcat pkg";
                pkgActionWindow.TextTitle.Text = "Get logcat:";
                pkgActionWindow.Send_Command.Content = "Logcat";
                pkgActionWindow.Show();
            }
        }
    }
}
