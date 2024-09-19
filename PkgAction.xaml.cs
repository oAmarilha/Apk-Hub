using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
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
    /// Interaction logic for PkgAction.xaml
    /// </summary>
    public partial class PkgAction : Window, IComponentConnector
    {
        private MainWindow? _mainWindow;
        private string _selectedDevice;
        private Window _calledWindow;
        public PkgAction(MainWindow mainWindow, Window calledWindow,string selectedDevice)
        {
            InitializeComponent();
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            _mainWindow = mainWindow;
            _selectedDevice = selectedDevice;
            _calledWindow = calledWindow;
            Owner = mainWindow;
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
                MethodInfo function = this.GetType().GetMethod(methodname, BindingFlags.NonPublic | BindingFlags.Instance);
                if (function != null)
                {
                    try
                    {
                        function.Invoke(this, null);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error calling function {methodname}: {ex.Message}");
                    }
                }
                else
                {
                    MessageBox.Show($"Method {methodname} not found.");
                }
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
            //textBox.Text = text.ToLower();

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
            if (App_Pkg.Text != string.Empty && _mainWindow != null)
            {
                _mainWindow.StatusText.Text = $"Uninstalling package {App_Pkg.Text}" + Environment.NewLine;
                Send_Command.IsEnabled = false;
                App_Pkg.IsEnabled = false;
                await AdbHelper.Instance.UninstallFunction(_mainWindow, _selectedDevice,App_Pkg.Text);
            }
            else
            {
                MessageBox.Show("You need inform the package to be uninstalled", "App not informed", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            Send_Command.IsEnabled = true;
            App_Pkg.IsEnabled = true;
            App_Pkg.Focus();
        }

        private void Logcat_Action()
        {
            if (App_Pkg.Text != string.Empty && _mainWindow != null)
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
            }

            Send_Command.IsEnabled = true;
            App_Pkg.IsEnabled = true;
            App_Pkg.Focus();
        }

        private async void ClearPkg_Action()
        {
            if (App_Pkg.Text != string.Empty && _mainWindow != null)
            {
                _mainWindow.StatusText.Text = $"Clearing package {App_Pkg.Text}" + Environment.NewLine;
                Send_Command.IsEnabled = false;
                App_Pkg.IsEnabled = false;
                await AdbHelper.Instance.ClearAppFunction(_mainWindow, _selectedDevice, App_Pkg.Text);
            }
            else
            {
                MessageBox.Show("You need inform the package to be cleared", "App not informed", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            Send_Command.IsEnabled = true;
            App_Pkg.IsEnabled = true;
            App_Pkg.Focus();
        }
    }
}
