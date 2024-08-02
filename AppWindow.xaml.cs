using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;

namespace ApkInstaller;

public partial class AppWindow : Window, IComponentConnector
{
	private Kids? _kidsWindow;

	private MainWindow? _mainWindow;

	private string _selectedDevice;

	private string _actionType;

	private bool _shell;

	private string _action;

	private LogcatWindow _logcatWindow;

	private CancellationTokenSource? _cancellationTokenSource;

	private Dictionary<string, (string, string)> appStringMap = new Dictionary<string, (string, string)>
	{
		{
			"Kids 3D",
			("com.sec.android.app.kids3d", "Crocro's Friend Village")
		},
		{
			"Music 3D",
			("com.sec.kidsplat.media.kidsmusic", "Lisa's Music Band")
		},
		{
			"Magic Voice",
			("com.sec.kidsplat.kidstalk", "My Magic Voice")
		},
		{
			"Adventure",
			("com.sec.kidsplat.kidsbcg", "Crocro's Adventure")
		},
		{
			"Browser",
			("com.sec.kidsplat.kidsbrowser", "My Browser")
		},
		{
			"Phone",
			("com.sec.kidsplat.phone", "My Phone")
		},
		{
			"Camera",
			("com.sec.kidsplat.camera", "My Camera")
		},
		{
			"My Gallery",
			("com.sec.kidsplat.kidsgallery", "My Gallery")
		},
		{
			"Cooki's",
			("br.org.sidi.kidsplat.collection", "Cooki's Collection")
		},
		{
			"Art Studio",
			("br.org.sidi.kidsplat.artstudio", "My Art Studio")
		},
		{
			"Trav. Buddies",
			("br.org.sidi.kidsplat.travel", "Travel Buddies")
		},
		{
			"Kids Home",
			("com.sec.android.app.kidshome", "Kids Home")
		}
	};

	public AppWindow(Kids kidsWindow, string selectedDevice, MainWindow mainWindow, string actionType, bool shell)
	{
		_kidsWindow = kidsWindow;
		_mainWindow = mainWindow;
		_shell = shell;
		_selectedDevice = selectedDevice;
		_actionType = actionType;
		InitializeComponent();
		if (_actionType == "pm clear")
		{
			_action = "Clear pkg action";
			Clear_Button.Content = "Clear";
		}
		else if (_actionType == "uninstall")
		{
			_action = "Uninstall app action";
			Clear_Button.Content = "Uninstall";
		}
		else if (_actionType == "logcat")
		{
			_action = "Logcat Action";
			Clear_Button.Content = "Logcat";
		}
		base.Title = _action;
		base.Closing += Cancellation_Token_Action;
	}

	private void CheckBox_Checked(object sender, RoutedEventArgs e)
	{
		if (!(_actionType == "logcat"))
		{
			return;
		}
		CheckBox checkBox = sender as CheckBox;
		foreach (object child in MainGrid.Children)
		{
			if (child is CheckBox checkBox2 && checkBox2 != checkBox)
			{
				checkBox2.IsEnabled = false;
			}
		}
	}

	private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
	{
		if (!(_actionType == "logcat"))
		{
			return;
		}
		foreach (object child in MainGrid.Children)
		{
			if (child is CheckBox checkBox)
			{
				checkBox.IsEnabled = true;
			}
		}
	}

    private async void ButtonSubmit_Click(object sender, RoutedEventArgs e)
    {
        List<string> list = new List<string>();
        List<string> appsName = new List<string>();
        foreach (object child in MainGrid.Children)
        {
            if (child is CheckBox { IsChecked: var isChecked } checkBox && isChecked == true && checkBox.Content is StackPanel stackPanel)
            {
                foreach (object child2 in stackPanel.Children)
                {
                    if (child2 is TextBlock textBlock && appStringMap.TryGetValue(textBlock.Text, out (string, string) value))
                    {
                        list.Add(value.Item1);
                        appsName.Add(value.Item2);
                    }
                }
            }
        }
        if (_actionType != "logcat")
        {
            Dispatcher.Invoke(() =>
            {
                _mainWindow.StatusText.Text = "Trying to start " + _action + " on the apps selected\n";
                _mainWindow.StatusText.Foreground = Brushes.White;
            });
            bool success = true;
            try
            {
                _cancellationTokenSource = new CancellationTokenSource();
                CancellationToken cancellationToken = _cancellationTokenSource.Token;
                Clear_Button.IsEnabled = false;
                foreach (string app in list)
                {
                    foreach (object child3 in MainGrid.Children)
                    {
                        if (child3 is CheckBox checkBox2)
                        {
                            checkBox2.IsEnabled = false;
                        }
                    }
                    await AdbHelper.Instance.RunAdbCommandAsync(_actionType + " " + app, _selectedDevice, _shell, output =>
                    {
                        Dispatcher.Invoke(() =>
                        {
                            if (output.Contains("Success"))
                            {
                                _mainWindow.StatusText.Text += $"\n{_action} executed on package {app}\n";
                                _mainWindow.UpdateStatusText(output);
                                _mainWindow.StatusText.Foreground = Brushes.Green;
                            }
                            else
                            {
                                _mainWindow.StatusText.Text += $"\n{_action} not executed on package {app}, check if the app is correctly installed.\n";
                                _mainWindow.UpdateStatusText(output);
                                _mainWindow.StatusText.Foreground = Brushes.Red;
                                success = false;
                            }
                            _mainWindow.StatusText.ScrollToEnd();
                        });
                    });
                }
                Clear_Button.IsEnabled = true;
                foreach (object child4 in MainGrid.Children)
                {
                    if (child4 is CheckBox checkBox3)
                    {
                        checkBox3.IsEnabled = true;
                    }
                }
            }
            catch (Exception ex4)
            {
                Exception ex3 = ex4;
                Exception ex = ex3;
                Dispatcher.Invoke(() =>
                {
                    TextBox statusText = _mainWindow.StatusText;
                    statusText.Text = statusText.Text + "Error: " + ex.Message + "\n";
                    _mainWindow.StatusText.Foreground = Brushes.Red;
                    _mainWindow.StatusText.ScrollToEnd();
                });
            }
            if (success)
            {
                MessageBox.Show(_action + " successfully executed on apps: " + string.Join(", ", appsName), "Action Executed", MessageBoxButton.OK, MessageBoxImage.Asterisk);
            }
            else
            {
                MessageBox.Show("An error occurred while trying to execute the command, check the output and try again.", "An error occurred", MessageBoxButton.OK, MessageBoxImage.Hand);
            }
        }
        else if (_logcatWindow == null || !_logcatWindow.IsVisible)
        {
            _logcatWindow = new LogcatWindow(_selectedDevice, list[0] ?? "");
            _logcatWindow.Owner = _mainWindow;
            if (_mainWindow.Top + _mainWindow.Height + 450.0 >= SystemParameters.PrimaryScreenHeight)
            {
                _logcatWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            }
            else
            {
                _logcatWindow.Top = _mainWindow.Top + _mainWindow.Height;
                _logcatWindow.Left = _mainWindow.Left;
            }
            _logcatWindow.PC_Info.Visibility = Visibility.Hidden;
            _logcatWindow.Show();
        }
        else
        {
            _logcatWindow.Focus();
        }
    }


    public void Cancellation_Token_Action(object? sender, CancelEventArgs e)
	{
		_mainWindow.Kids_Button.IsEnabled = true;
		_kidsWindow?.Show();
		_cancellationTokenSource?.Cancel();
		if (_logcatWindow != null)
		{
			_logcatWindow.Close();
		}
	}
}