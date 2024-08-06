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
    }
}
