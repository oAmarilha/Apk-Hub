using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Media;

namespace ApkInstaller;

public partial class LogcatWindow : Window, IComponentConnector
{
	private string _selectedDevice;

	private Process? _process;

	private string _filter;

	private MainWindow _mainWindow;

	private Window _calledWindow;

	private string _accountToken;

	private string _deviceId;

	private string _accountId;

	private string _accountTokenUrl;

	private string _clientId;

	public LogcatWindow(MainWindow mainWindow, Window calledWindow, string selectedDevice, string filter)
	{
		InitializeComponent();
		_mainWindow = mainWindow;
		_selectedDevice = selectedDevice;
		_calledWindow = calledWindow;
		_filter = filter;
		ClearLogcat();
		StartLogcat();
		_mainWindow.Hide();
		_calledWindow.Hide();
		base.Closing += Closing_Window;
	}

	private async void ClearLogcat()
	{
		try
		{
			await Task.Run(delegate
			{
				Process process = new Process();
				process.StartInfo.FileName = "adb";
				process.StartInfo.Arguments = "-s " + _selectedDevice + " logcat -c";
				process.StartInfo.UseShellExecute = false;
				process.StartInfo.CreateNoWindow = true;
				process.Start();
				process.WaitForExit();
			});
		}
		catch (Exception ex)
		{
			MessageBox.Show("Error clearing logcat: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Hand);
		}
	}

	private async void StartLogcat()
	{
		try
		{
			await Task.Run(delegate
			{
				_process = new Process();
				_process.StartInfo.FileName = "adb";
				_process.StartInfo.Arguments = "-s " + _selectedDevice + " logcat";
				_process.StartInfo.RedirectStandardOutput = true;
				_process.StartInfo.RedirectStandardError = true;
				_process.StartInfo.UseShellExecute = false;
				_process.StartInfo.CreateNoWindow = true;
				_process.OutputDataReceived += delegate(object sender, DataReceivedEventArgs e)
				{
					if (e.Data != null)
					{
						ExtractValuesFromLog(e.Data);
						if (e.Data.Contains(_filter))
						{
							base.Dispatcher.Invoke(delegate
							{
								LogcatTextBox.AppendText(e.Data + Environment.NewLine);
								LogcatTextBox.ScrollToEnd();
							});
						}
					}
				};
				_process.Start();
				_process.BeginOutputReadLine();
				_process.WaitForExit();
			});
		}
		catch (Exception ex)
		{
			MessageBox.Show("Error starting logcat: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Hand);
		}
	}

	private void ExtractValuesFromLog(string logLine)
	{
		if (_accountToken == null)
		{
			Match match = Regex.Match(logLine, "Account-Token:\\s*(\\S+)");
			if (match.Success)
			{
				_accountToken = match.Groups[1].Value;
				base.Dispatcher.Invoke(() => AccountTokenText.Text = _accountToken);
			}
		}
		if (_deviceId == null)
		{
			Match match2 = Regex.Match(logLine, "Device-Id:\\s*(\\S+)");
			if (match2.Success)
			{
				_deviceId = match2.Groups[1].Value;
				base.Dispatcher.Invoke(() => DeviceIdText.Text = _deviceId);
			}
		}
		if (_accountId == null)
		{
			Match match3 = Regex.Match(logLine, "Account-Id:\\s*(\\S+)");
			if (match3.Success)
			{
				_accountId = match3.Groups[1].Value;
				base.Dispatcher.Invoke(() => AccountIdText.Text = _accountId);
			}
		}
		if (_accountTokenUrl == null)
		{
			Match match4 = Regex.Match(logLine, "Account-Token-Url:\\s*(\\S+)");
			if (match4.Success)
			{
				_accountTokenUrl = match4.Groups[1].Value;
				base.Dispatcher.Invoke(() => AccountTokenUrlText.Text = _accountTokenUrl);
			}
		}
		if (_clientId != null)
		{
			return;
		}
		Match match5 = Regex.Match(logLine, "Client-Id:\\s*(\\S+)");
		if (match5.Success)
		{
			_clientId = match5.Groups[1].Value;
			base.Dispatcher.Invoke(() => ClientIdText.Text = _clientId);
		}
	}

	private void StartStopButton_Click(object sender, RoutedEventArgs e)
	{
		if (StartStopButton.Content.ToString() == "Stop")
		{
			StopLogcat();
			return;
		}
		ClearLogcat();
		LogcatTextBox.Text = "";
		StartStopButton.Content = "Stop";
		StartStopButton.Background = Brushes.Red;
		StartLogcat();
	}

	private void StopLogcat()
	{
		if (_process != null && !_process.HasExited)
		{
			_process.Kill();
			_process.Dispose();
			_process = null;
		}
		StartStopButton.Content = "Start";
		StartStopButton.Background = Brushes.Green;
	}

	private void SaveLog_Click(object sender, RoutedEventArgs e)
	{
		string value = DateTime.Now.ToString().Replace("/", "-").Replace(":", "-")
			.Replace(" ", "_");
		Directory.CreateDirectory($"./log/Logcat/{_filter}/{value}/");
		try
		{
			string contents = LogcatTextBox.Text.ToString();
			string text = $"{Directory.GetCurrentDirectory()}\\log\\Logcat\\{_filter}\\{value}";
			File.WriteAllText($"{text}\\{_filter}_{value}.txt", contents);
			MessageBoxResult result = MessageBox.Show($"File Save in: {text}\\{_filter}_{value}.txt\nDo you want to open it?", "File Saved", MessageBoxButton.YesNo, MessageBoxImage.Asterisk);
			if (result == MessageBoxResult.Yes)
			{
                Process.Start(new ProcessStartInfo()
                {
                    FileName = text,
                    UseShellExecute = true,
                    Verb = "open"
                });
            }
		}
		catch (Exception ex)
		{
			MessageBox.Show("Error saving the log file: " + ex.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Hand);
		}
	}

    public void Closing_Window(object? sender, CancelEventArgs e)
    {
        _mainWindow.Show();
		_calledWindow.Show();
    }
}
