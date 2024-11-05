using ApkInstaller.Helper_classes;
using System.ComponentModel;
using System.Media;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;

namespace ApkInstaller
{
    /// <summary>
    /// Interaction logic for PkgAction.xaml
    /// </summary>
    public partial class PkgAction : Window, IComponentConnector
    {
        private MainWindow _mainWindow;
        private string _selectedDevice;
        private Window _calledWindow;
        public PkgAction(MainWindow mainWindow, Window calledWindow, string selectedDevice)
        {
            InitializeComponent();
            _mainWindow = mainWindow;
            Owner = mainWindow;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            _selectedDevice = selectedDevice;
            _calledWindow = calledWindow;
            App_Pkg.Focus();
            _calledWindow.Hide();
            base.Closing += PkgAction_Closing;
        }

        private void PkgAction_Closing(object? sender, CancelEventArgs e)
        {
            _calledWindow.Show();
            if (_mainWindow != null)
            {
                _mainWindow.Activate();
            }
        }

        private void Action_Button_Click(object sender, RoutedEventArgs e)
        {
            Action_Click(Send_Command.Content.ToString() + "_Action");
        }

        private void Action_Click(string methodname)
        {
            MethodInfo? function = this.GetType().GetMethod(methodname, BindingFlags.NonPublic | BindingFlags.Instance);
            if (function != null)
            {
                try
                {
                    function.Invoke(this, null);
                }
                catch (Exception ex)
                {
                    _mainWindow.ShowMessage($"Error calling function {methodname}: {ex.Message}");
                }
            }
            else
            {
                _mainWindow.ShowMessage($"Method {methodname} not found.");
            }
        }

        private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("^[a-zA-Z.]+$");
            e.Handled = !regex.IsMatch(e.Text);   
        }

        private void TextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            TextBox textBox = App_Pkg;

            // Verifica se a tecla de espaço foi pressionada
            if (e.Key == Key.Space)
            {
                // Bloqueia espaço no início do TextBox
                if (textBox.Text.Length == 0)
                {
                    e.Handled = true;
                }
                // Bloqueia dois espaços consecutivos
                else if (textBox.Text.EndsWith(" "))
                {
                    e.Handled = true;
                }
            }
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
            if (e.Key == Key.Enter && sender is TextBox textBox)
            {
                Action_Click(Send_Command.Content.ToString() + "_Action");
            }
        }

        private async void Uninstall_Action()
        {
            if (App_Pkg.Text != string.Empty)
            {
                _mainWindow.StatusText.Text = $"Uninstalling package {App_Pkg.Text}" + Environment.NewLine;
                Send_Command.IsEnabled = false;
                App_Pkg.IsEnabled = false;
                await AdbHelper.Instance.UninstallFunction(_selectedDevice, App_Pkg.Text);
            }
            else
            {
                _mainWindow.ShowMessage("You need inform the package to be uninstalled", "App not informed", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            Send_Command.IsEnabled = true;
            App_Pkg.IsEnabled = true;
            App_Pkg.Focus();
        }

        private void Logcat_Action()
        {
            _mainWindow.StatusText.Text = $"Trying to get logcat from {App_Pkg.Text}" + Environment.NewLine;
            Send_Command.IsEnabled = false;
            App_Pkg.IsEnabled = false;
            LogcatWindow logcatWindow = new LogcatWindow(_mainWindow, this, _selectedDevice, App_Pkg.Text);
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
            Send_Command.IsEnabled = true;
            App_Pkg.IsEnabled = true;
            App_Pkg.Focus();
        }

        private async void ClearPkg_Action()
        {
            if (App_Pkg.Text != string.Empty)
            {
                _mainWindow.StatusText.Text = $"Clearing package {App_Pkg.Text}" + Environment.NewLine;
                Send_Command.IsEnabled = false;
                App_Pkg.IsEnabled = false;
                await AdbHelper.Instance.ClearAppFunction(_selectedDevice, App_Pkg.Text);
            }
            else
            {
                _mainWindow.ShowMessage("You need inform the package to be cleared", "App not informed", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            Send_Command.IsEnabled = true;
            App_Pkg.IsEnabled = true;
            App_Pkg.Focus();
        }

        private async void Run_Action()
        {
            object? command_output = null;
            _mainWindow.UpdateStatusText(clear: true);

            if (App_Pkg.Text != string.Empty)
            {
                string input = App_Pkg.Text.Trim();
                if (input != "shell" && !input.Contains("logcat"))
                {
                    _mainWindow.UpdateStatusText($"Executing '{App_Pkg.Text.Trim()}' command\n", clear: true);
                    Send_Command.IsEnabled = false;
                    App_Pkg.IsEnabled = false;
                    await AdbHelper.Instance.RunAdbCommandAsync(App_Pkg.Text.Trim(), output =>
                    {
                        _mainWindow.UpdateStatusText(output);
                        command_output += output;
                    }, _selectedDevice);
                    if (command_output is string str && (str.ToLower().Contains("unknown command") || str.ToLower().Contains("inaccessible or not found") || str.ToLower().Contains("error:")))
                    {
                        command_output = null;
                    }
                }
                if (command_output == null)
                {
                    _mainWindow.UpdateStatusText("Invalid command",isError: true);
                    SystemSounds.Exclamation.Play();
                }
            }
            else
            {
                _mainWindow.ShowMessage("You need inform the command to be executed", "Command not informed", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            Send_Command.IsEnabled = true;
            App_Pkg.IsEnabled = true;
            App_Pkg.Focus();
        }
    }
}
