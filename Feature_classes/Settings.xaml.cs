using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Media;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using ApkInstaller.Helper_classes;

namespace ApkInstaller;

public partial class Settings : Window, IComponentConnector
{
	private MainWindow _mainWindow;

	private LogcatWindow? logcatWindow;

	private string _selectedDevice;

	private CancellationTokenSource? _cancellationTokenSource;

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
        await AdbHelper.Instance.RunAdbCommandAsync(command, output =>
        {
            _mainWindow.UpdateStatusText(output);
        }, _selectedDevice, shell);
	}

	private async void RemountButton_Click(object sender, RoutedEventArgs e)
	{
		string command = "remount";
		await SendCommandButton(command, shell: false);
		await SendCommandButton(command, shell: false);
		bool status = _mainWindow.StatusText.Text.Contains("Remount succeeded");
        _mainWindow.UpdateStatusText(isError: !status, isSuccess: status);
    }

    private async Task PushParentalApk(bool install = false)
	{
		string type = "";
		CancellationToken token = new CancellationTokenSource().Token;
		await AdbHelper.Instance.RunAdbCommandAsync("getprop ro.build.characteristics", output =>
        {
			base.Dispatcher.Invoke(() =>
			{
				type += output;
			});
        }, _selectedDevice, shell: true);
		string apkFile = (from stackPanel in _mainWindow.ApkFilesList.Items.OfType<StackPanel>()
			select stackPanel.Tag.ToString()).FirstOrDefault((string filename) => Path.GetFileName(filename)?.StartsWith("Parental") ?? false) ?? string.Empty;
		string careSampleFile = (from stackPanel in _mainWindow.ApkFilesList.Items.OfType<StackPanel>()
			select stackPanel.Tag.ToString()).FirstOrDefault((string filename) => Path.GetFileName(filename)?.StartsWith("CareSample") ?? false) ?? string.Empty;
		if (string.IsNullOrEmpty(apkFile))
		{
			SystemSounds.Exclamation.Play();
			_mainWindow.UpdateStatusText("No APK file starting with 'Parental' found.\nPlease add a Parental Care apk file and try again", isError: true);
			return;
		}
		await SendCommandButton("root", shell: false);
		await SendCommandButton("remount", shell: false);
		await SendCommandButton("remount", shell: false);
		string apkName = Path.GetFileName(apkFile);
		if (!type.Contains("watch") || (!type.Contains("default")))
		{
			await SendCommandButton("rm -r /system/priv-app/ParentalCare", shell: true);
			await SendCommandButton("push \"" + apkFile + "\" /system/priv-app/ParentalCare/" + apkName, shell: false);
		}
		else
		{
			await SendCommandButton("rm -r /system/priv-app/ParentalCareWatch/", shell: true);
			await SendCommandButton("push \"" + apkFile + "\" /system/priv-app/ParentalCareWatch/" + apkName, shell: false);
		}
		_mainWindow.UpdateStatusText("\nParental pushed to system folder");
		if (_mainWindow.StatusText.Text.Contains("1 file pushed"))
		{
			_mainWindow.UpdateStatusText("Success", isSuccess: true);
        }
		else
		{
			SystemSounds.Exclamation.Play();
			_mainWindow.UpdateStatusText("Failed, check the device and try again", isError: true);
			return;
        }
        if (install)
        {
            await SendCommandButton("install -r -d \"" + apkFile + "\"", shell: false);
            if (!string.IsNullOrEmpty(careSampleFile))
            {
                await SendCommandButton("install -r -d \"" + careSampleFile + "\"", shell: false);
            }
            else
            {
                SystemSounds.Exclamation.Play();
                _mainWindow.UpdateStatusText("No APK file starting with 'CareSample' found.\nPlease add a Care Sample apk and try again", isError: true, clear: true);
				return;
            }
            _mainWindow.UpdateStatusText(isSuccess: true);
        }
    }

	private async void PushParentalApk_Click(object sender, RoutedEventArgs e)
	{
		_mainWindow.UpdateStatusText(clear : true);
		await PushParentalApk();
	}

	private async void UninstallParental_Click(object sender, RoutedEventArgs e)
	{
		_mainWindow.UpdateStatusText(clear : true);
		string command = "uninstall com.samsung.android.app.parentalcare";
		string caresampleuninstall = "uninstall com.samsung.android.app.caresample";
		_mainWindow.UpdateStatusText("Uninstalling Parental Care...");
		await SendCommandButton(command, shell: false);
		_mainWindow.UpdateStatusText("Uninstalling Care Sample...");
		await SendCommandButton(caresampleuninstall, shell: false);
		bool status = _mainWindow.StatusText.Text.Contains("Success");
        _mainWindow.UpdateStatusText(isError: !status, isSuccess: status);
	}

	private async void InstallPC_Click(object sender, RoutedEventArgs e)
	{
		_mainWindow.UpdateStatusText("Initializing Parental Care full installation", clear : true);
		await PushParentalApk(install: true);
	}

	private async void ClearPackage_Click(object sender, RoutedEventArgs e)
	{
		_mainWindow.UpdateStatusText("Initializing package data clear, waiting...");
		_mainWindow.UpdateStatusText("Clearing Parental Care app...");
		await SendCommandButton("pm clear com.samsung.android.app.parentalcare", shell: true);
		_mainWindow.UpdateStatusText("Clearing Care Sample app...");
		await SendCommandButton("pm clear com.samsung.android.app.caresample", shell: true);
		_mainWindow.StatusText.Foreground = (_mainWindow.StatusText.Text.Contains("Success") ? Brushes.Green : Brushes.Red);
	}

	private void LogcatButton_Click(object sender, RoutedEventArgs e)
	{
		if (logcatWindow == null)
		{
			logcatWindow = new LogcatWindow(_mainWindow ,this, _selectedDevice, "com.samsung.android.app.parentalcare");
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
            logcatWindow.Closing += LogcatWindow_Closing;
		}
		else
		{
			logcatWindow.Focus();
		}
	}

    private void LogcatWindow_Closing(object? sender, CancelEventArgs e)
    {
		logcatWindow = null;
    }

    private void ClosingSettings(object? sender, CancelEventArgs e)
	{
		if (logcatWindow != null)
		{
			logcatWindow.Close();
		}
		_mainWindow.ActivateDevicesBox();
		AdbHelper.Instance.StopCommand();
		_mainWindow.Browse_Button.IsEnabled = true;
		_mainWindow.Kids_Button.IsEnabled = true;
		_mainWindow.ApkFilesList.IsEnabled = true;
		_mainWindow.Activate();
	}
}
