using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ApkInstaller
{
    /// <summary>
    /// Interaction logic for Uninstall.xaml
    /// </summary>
    public partial class Uninstall : Window, IComponentConnector
    {
        private MainWindow? _mainWindow;
        private string _selectedDevice;
        public Uninstall(MainWindow mainWindow, string selectedDevice)
        {
            _mainWindow = mainWindow;
            _selectedDevice = selectedDevice;
            Owner = mainWindow;
            InitializeComponent();
            App_Pkg.Focus();
        }

        private async void Uninstall_Click(object sender, RoutedEventArgs e)
        {
            Uninstall_Action();
        }

        private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("^[a-zA-Z.]+$");
            e.Handled = !regex.IsMatch(e.Text);
        }
        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            int caretIndex = textBox.CaretIndex;
            string text = textBox.Text;

            // Convert the text to lowercase
            textBox.Text = text.ToLower();

            // Set the caret index back to its original position
            textBox.CaretIndex = caretIndex;
        }

        private void Enter_Key(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Uninstall_Action();
            }
        }

        private async void Uninstall_Action()
        {
            if (App_Pkg.Text != string.Empty && _mainWindow != null)
            {
                Send_Command.IsEnabled = false;
                App_Pkg.IsEnabled = false;
                _mainWindow.StatusText.Text = "";
                string pkg = App_Pkg.Text;
                string result = "";
                string pkgOutput = "";
                string apkName = "";
                string labelName = "";
                string capturedText = "";
                await AdbHelper.Instance.RunAdbCommandAsync($"pm list package -f {pkg}", _selectedDevice, true, output =>
                {
                    pkgOutput += output;
                    _mainWindow.UpdateStatusText(output);
                });
                Regex regex = new Regex($@"package:(/data/app/~~.*?)/base\.apk={pkg}");
                Match match = regex.Match(pkgOutput);
                if (match.Success)
                {
                    Directory.CreateDirectory("log/apk");
                    apkName = match.Groups[1].Value + "/base.apk";
                    await AdbHelper.Instance.RunAdbCommandAsync($"pull {apkName} log/apk/test.apk", _selectedDevice, false, output => { });
                    
                    await AdbHelper.Instance.RunCommandAsync("aapt", $"d badging log/apk/test.apk",output => { labelName += output; });
                    string pattern = @"application-label-en-US:'([^']*)'";
                    Regex name = new Regex(pattern);

                    Match findName = name.Match(labelName);
                    if (findName.Success)
                    {
                        capturedText = findName.Groups[1].Value;
                        _mainWindow.UpdateStatusText(capturedText);
                    }
                    Directory.Delete("log/apk", true);
                }
                await AdbHelper.Instance.RunAdbCommandAsync($"uninstall {pkg}", _selectedDevice, false, output =>
                {
                    _mainWindow.UpdateStatusText(output);
                    result += output;
                });
                Send_Command.IsEnabled = true;
                App_Pkg.IsEnabled = true;
                App_Pkg.Focus();
                if (result.Contains("Success"))
                {
                    capturedText = Encoding.UTF8.GetString(Encoding.GetEncoding("ISO-8859-1").GetBytes(capturedText));
                    _mainWindow.UpdateStatusText($"The app {capturedText} was succesfully uninstalled");
                    _mainWindow.StatusText.Foreground = Brushes.Green;
                }
                else
                {
                    _mainWindow.UpdateStatusText($"The app {pkg} was not uninstalled, check the package's name and try again");
                    _mainWindow.StatusText.Foreground = Brushes.Red;
                }
            }
            else
            {
                MessageBox.Show("You need inform the package to be uninstalled", "App not informed", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}
