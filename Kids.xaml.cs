using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;

namespace ApkInstaller;

public partial class Kids : Window, IComponentConnector
{
	private MainWindow _mainWindow;

	private string _selectedDevice;

	private CancellationTokenSource _cancellationTokenSource;

	private AppWindow? _appWindow;

	private string localFile = Directory.GetCurrentDirectory() + "\\log";

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
		_cancellationTokenSource = new CancellationTokenSource();
		CancellationToken token = _cancellationTokenSource.Token;
		try
		{
			await AdbHelper.Instance.RunAdbCommandAsync("logcat -c", _selectedDevice, shell: true, output =>
			{
                if (string.IsNullOrEmpty(output))
                {
                    _mainWindow.StatusText.Text = "";
                    _mainWindow.StatusText.Text = "Logcat cleared succesfully";
                    _mainWindow.StatusText.Foreground = Brushes.Green;
                }
                else
                {
                    _mainWindow.StatusText.Text = "Logcat not clared";
                    _mainWindow.StatusText.Foreground = Brushes.Red;
                }
            });
		}
		catch (Exception ex4)
		{
			Exception ex3 = ex4;
			Exception ex = ex3;
			base.Dispatcher.Invoke(delegate
			{
				TextBox statusText = _mainWindow.StatusText;
				statusText.Text = statusText.Text + "Error: " + ex.Message + "\n";
				_mainWindow.StatusText.Foreground = Brushes.Red;
				_mainWindow.StatusText.ScrollToEnd();
			});
		}
	}

	private void LogcatView_Button(object sender, RoutedEventArgs e)
	{
		OpenAppWindow("logcat", shell: false);
	}

	private async void RealTimeScreen()
	{
		await AdbHelper.Instance.RunCommandAsync("scrcpy", "-s \"" + _selectedDevice + "\"", delegate(string output)
		{
			base.Dispatcher.Invoke(delegate
			{
				_mainWindow.UpdateStatusText(output);
			});
		});
	}

	private void StopScreenRecording()
	{
		_cancellationTokenSource.Cancel();
		_cancellationTokenSource = new CancellationTokenSource();
	}

	private async void ScreenRecord_Button(object sender, RoutedEventArgs e)
	{
		if (Record_Screen.Content.ToString() == "Record")
		{
			RealTimeScreen();
			Record_Screen.Content = "Stop";
			Record_Screen.Background = Brushes.Red;
			await AdbHelper.Instance.StartScreenRecording(_mainWindow, _selectedDevice, localFile);
		}
		else
		{
			Record_Screen.Content = "Record";
			Record_Screen.Background = Brushes.Green;
			StopScreenRecording();
		}
	}
}