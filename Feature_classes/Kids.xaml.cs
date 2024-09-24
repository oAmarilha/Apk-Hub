using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
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

	public Kids(MainWindow mainWindow, string selectedDevice)
	{
		InitializeComponent();
		_mainWindow = mainWindow;
		_selectedDevice = selectedDevice;
		_cancellationTokenSource = new CancellationTokenSource();
		base.Owner = _mainWindow;
        base.Closing += ClosingWindow;
	}

	private void ClosingWindow(object? sender, CancelEventArgs e)
    {
        _mainWindow.ActivateDevicesBox();
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
		_appWindow = new AppWindow(this, _selectedDevice, _mainWindow, actionType, shell);
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
			await AdbHelper.Instance.RunAdbCommandAsync("logcat -c", _selectedDevice, shell: true, output =>
			{
				logcat += output;
            });

            if (string.IsNullOrEmpty(logcat))
            {
                _mainWindow.UpdateStatusText("Logcat cleared succesfully", isSuccess: true , clear: true);
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
}