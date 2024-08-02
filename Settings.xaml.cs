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

namespace ApkInstaller;

public partial class Settings : Window, IComponentConnector
{
	private MainWindow _mainWindow;

	private LogcatWindow logcatWindow;

	private string _selectedDevice;

	private CancellationTokenSource _cancellationTokenSource;

	private string localFile = Directory.GetCurrentDirectory() + "\\log";

	public Settings(MainWindow mainWindow, string selectedDevice)
	{
		InitializeComponent();
		_mainWindow = mainWindow;
		_selectedDevice = selectedDevice;
		_cancellationTokenSource = new CancellationTokenSource();
		base.Owner = _mainWindow;
		base.Closing += ClosingSettings;
	}

	public async Task SendCommandButton(string command, bool shell)
	{
		await AdbHelper.Instance.RunAdbCommandAsync(command, _selectedDevice, shell, output =>
        {
            _mainWindow.UpdateStatusText(output);
        });
	}

	private async void RemountButton_Click(object sender, RoutedEventArgs e)
	{
		_mainWindow.StatusText.Text = "";
		string command = "remount";
		await SendCommandButton(command, shell: false);
		await SendCommandButton(command, shell: false);
		_mainWindow.StatusText.Foreground = (_mainWindow.StatusText.Text.Contains("Remount succeeded") ? Brushes.Green : Brushes.Red);
	}

	private async Task PushParentalApk(bool install = false)
	{
		string type = "";
		CancellationToken token = new CancellationTokenSource().Token;
		await AdbHelper.Instance.RunAdbCommandAsync("getprop ro.build.characteristics", _selectedDevice, shell: true, output =>
        {
            type = output;
        });
		string apkFile = (from stackPanel in _mainWindow.ApkFilesList.Items.OfType<StackPanel>()
			select stackPanel.Tag.ToString()).FirstOrDefault((string filename) => Path.GetFileName(filename)?.StartsWith("Parental") ?? false) ?? string.Empty;
		string careSampleFile = (from stackPanel in _mainWindow.ApkFilesList.Items.OfType<StackPanel>()
			select stackPanel.Tag.ToString()).FirstOrDefault((string filename) => Path.GetFileName(filename)?.StartsWith("CareSample") ?? false) ?? string.Empty;
		if (string.IsNullOrEmpty(apkFile))
		{
			SystemSounds.Exclamation.Play();
			_mainWindow.UpdateStatusText("No APK file starting with 'Parental' found.\nPlease add a Parental Care apk file and try again");
			_mainWindow.StatusText.Foreground = Brushes.Red;
			return;
		}
		await SendCommandButton("root", shell: false);
		await SendCommandButton("remount", shell: false);
		await SendCommandButton("remount", shell: false);
		string apkName = Path.GetFileName(apkFile);
		if (!type.Contains("watch") && !type.Contains("default"))
		{
			await SendCommandButton("shell rm -r /system/priv-app/ParentalCare", shell: true);
			await SendCommandButton("push \"" + apkFile + "\" /system/priv-app/ParentalCare/" + apkName, shell: false);
		}
		else
		{
			await SendCommandButton("shell rm -r /system/priv-app/ParentalCareWatch/", shell: true);
			await SendCommandButton("push \"" + apkFile + "\" /system/priv-app/ParentalCareWatch/" + apkName, shell: false);
		}
		_mainWindow.UpdateStatusText("\nParental pushed to system folder");
		if (install)
		{
			await SendCommandButton("install -r -d \"" + apkFile + "\"", shell: false);
			if (!string.IsNullOrEmpty(careSampleFile))
			{
				await SendCommandButton("install -r -d \"" + careSampleFile + "\"", shell: false);
			}
			else
			{
				_mainWindow.StatusText.Text = "";
				_mainWindow.UpdateStatusText("No APK file starting with 'CareSample' found.\nPlease add a Care Sample apk and try again");
				_mainWindow.StatusText.Foreground = Brushes.Red;
			}
			_mainWindow.StatusText.Foreground = (_mainWindow.StatusText.Text.Contains("Success") ? Brushes.Green : Brushes.Red);
		}
		if (_mainWindow.StatusText.Text.Contains("1 file pushed"))
		{
			_mainWindow.StatusText.Foreground = Brushes.Green;
			_mainWindow.StatusText.Text = "Success";
		}
		else
		{
			SystemSounds.Exclamation.Play();
			_mainWindow.StatusText.Foreground = Brushes.Red;
			_mainWindow.StatusText.Text = "Failed, check the device and try again";
		}
	}

	private async void PushParentalApk_Click(object sender, RoutedEventArgs e)
	{
		_mainWindow.StatusText.Text = "";
		await PushParentalApk();
	}

	private async void UninstallParental_Click(object sender, RoutedEventArgs e)
	{
		_mainWindow.StatusText.Text = "";
		string command = "uninstall com.samsung.android.app.parentalcare";
		string caresampleuninstall = "uninstall com.samsung.android.app.caresample";
		_mainWindow.UpdateStatusText("Uninstalling Parental Care...");
		await SendCommandButton(command, shell: false);
		_mainWindow.UpdateStatusText("Uninstalling Care Sample...");
		await SendCommandButton(caresampleuninstall, shell: false);
		_mainWindow.StatusText.Foreground = (_mainWindow.StatusText.Text.Contains("Success") ? Brushes.Green : Brushes.Red);
	}

	private void RemoteScreen_Click(object sender, RoutedEventArgs e)
	{
		if (Share_Button.Content.ToString() == "Mirroring")
		{
			ScreenRecordButton.IsEnabled = false;
			Share_Button.Content = "Stop";
			Share_Button.Background = Brushes.Red;
			RealTimeScreen();
		}
		else
		{
			ScreenRecordButton.IsEnabled = true;
			Share_Button.Content = "Mirroring";
			Share_Button.Background = Brushes.LightGray;
			EndRealTimeScreen();
		}
	}

	private async void InstallPC_Click(object sender, RoutedEventArgs e)
	{
		_mainWindow.StatusText.Text = "";
		_mainWindow.UpdateStatusText("Initializing Parental Care full installation");
		await PushParentalApk(install: true);
	}

	private async void ClearPackage_Click(object sender, RoutedEventArgs e)
	{
		_mainWindow.StatusText.Text = "Initializing package data clear, waiting...\n";
		_mainWindow.UpdateStatusText("Clearing Parental Care app...");
		await SendCommandButton("pm clear com.samsung.android.app.parentalcare", shell: true);
		_mainWindow.UpdateStatusText("Clearing Care Sample app...");
		await SendCommandButton("pm clear com.samsung.android.app.caresample", shell: true);
		_mainWindow.StatusText.Foreground = (_mainWindow.StatusText.Text.Contains("Success") ? Brushes.Green : Brushes.Red);
	}

	private async void RealTimeScreen()
	{
		await AdbHelper.Instance.RunAdbCommandAsync("", _selectedDevice, false, output =>
		{
			_mainWindow.UpdateStatusText(output);
		}, "scrcpy");
	}

	private void EndRealTimeScreen()
	{
		AdbHelper.Instance.StopCommand();
	}

	private void LogcatButton_Click(object sender, RoutedEventArgs e)
	{
		if (logcatWindow == null || !logcatWindow.IsVisible)
		{
			logcatWindow = new LogcatWindow(_selectedDevice, "com.samsung.android.app.parentalcare");
			logcatWindow.Owner = _mainWindow;
			if (_mainWindow.Top + _mainWindow.Height + 450.0 >= SystemParameters.PrimaryScreenHeight)
			{
				logcatWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
			}
			else
			{
				logcatWindow.Top = _mainWindow.Top + _mainWindow.Height;
				logcatWindow.Left = _mainWindow.Left;
			}
			logcatWindow.Show();
		}
		else
		{
			logcatWindow.Focus();
		}
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

	private void ClosingSettings(object? sender, CancelEventArgs e)
	{
		if (logcatWindow != null)
		{
			logcatWindow.Close();
		}
		_mainWindow.ActivateDevicesBox();
		_cancellationTokenSource.Cancel();
		_mainWindow.Browse_Button.IsEnabled = true;
		_mainWindow.Kids_Button.IsEnabled = true;
		_mainWindow.ApkFilesList.IsEnabled = true;
		_mainWindow.Install_Button.IsEnabled = true;
		_mainWindow.Activate();
	}
}
