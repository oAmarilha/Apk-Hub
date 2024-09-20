using MahApps.Metro.Controls;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Media;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using Microsoft.Win32;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Windows.Input;

namespace ApkInstaller;

public partial class MainWindow : MetroWindow, IComponentConnector
{
	private Settings? settingsWindow;

	private Kids? kidsWindow;

	private UsbDeviceNotifier? usbDeviceNotifier;

	private More? moreWindow;

	private bool loopCancelation = false;

	private bool success;

	public string appPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}\\ApkHub\\Log";

	public MainWindow()
	{
		InitializeComponent();
        this.WindowTitleBrush = new SolidColorBrush(Color.FromRgb(75, 10, 198));
        this.BorderBrush = new SolidColorBrush(Color.FromRgb(75, 10, 198));
        base.Loaded += MainWindow_Loaded;
		Install_Button.Content = "Install APKs";
		base.Closing += MainWindow_Closing;
	}

	private void MainWindow_Loaded(object sender, RoutedEventArgs e)
	{
		PopulateDevices();
		Directory.CreateDirectory(appPath);
		usbDeviceNotifier = new UsbDeviceNotifier(this);
		usbDeviceNotifier.UsbDeviceChanged += OnUsbDeviceChanged!;
    }

    private async void OnUsbDeviceChanged(object sender, EventArgs e)
	{
		await Task.Delay(1000);
		PopulateDevices();
	}

	public async void PopulateDevices()
	{
		DevicesComboBox.ItemsSource = "";
		Install_Button.IsEnabled = false;
		StatusText.Text = "Checking devices connected...\n";
		StatusText.Foreground = Brushes.White;
		await Task.Run(delegate
		{
			Dictionary<string, string> connectedDevices = GetConnectedDevices();
			List<string> deviceList = new List<string>();
			foreach (KeyValuePair<string, string> current in connectedDevices)
			{
				string item = current.Value + " (" + current.Key + ")";
				deviceList.Add(item);
			}
			base.Dispatcher.Invoke(delegate
			{
				DevicesComboBox.ItemsSource = deviceList;
				int count = deviceList.Count;
				if (count == 1)
				{
					DevicesComboBox.SelectedItem = deviceList[0];
				}
				StatusText.Text = ((count >= 1) ? (count + " Device(s) Connected\n") : "No Device Connected\n");
				StatusText.Foreground = ((count >= 1) ? Brushes.Green : Brushes.Red);
				//Install_Button.IsEnabled = count >= 1;
				if (ApkFilesList.Items.Count > 0 && count > 0)
				{
					Install_Button.IsEnabled = true;
				}
				if (settingsWindow != null && DevicesComboBox.SelectedItem == null)
				{
					settingsWindow.Close();
				}
				else if (kidsWindow != null && DevicesComboBox.SelectedItem == null)
				{
					kidsWindow.Close();
				}
				else if (moreWindow != null && DevicesComboBox.SelectedItem == null){
					moreWindow.Close();
				}
			});
		});
	}

	private string? CheckDeviceComboBox()
	{
		if (DevicesComboBox.SelectedItem == null)
		{
			return null;
		}
		return DevicesComboBox.SelectedItem.ToString();
	}

	private void RefreshButton_Click(object sender, RoutedEventArgs e)
	{
		PopulateDevices();
	}

	private void ChangeButtonVisibility(bool change)
	{
        foreach (var child in MainWindowGrid.Children)
        {
            if (child is StackPanel stackPanel)
            {
                foreach (var stackChild in stackPanel.Children)
                {
                    if (stackChild is Button button)
                    {
                        button.IsEnabled = change;
						if (DevicesComboBox.Items.Count == 0) 
						{
							Install_Button.IsEnabled = false;
						}
                    }
                }
            }
        }
    }

	private void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
		ChangeButtonVisibility(false);
        List<string> error = new List<string>();
        OpenFileDialog openFileDialog = new OpenFileDialog
		{
			Multiselect = true,
			Filter = "APK files (*.apk)|*.apk"
		};
		if (openFileDialog.ShowDialog() == true)
		{
			UpdateStatusText("Loading file(s) selected",clear:true);
			string[] fileNames = openFileDialog.FileNames;
			foreach (string filename in fileNames)
			{
                string add = AddApkFile(filename);
                if (!string.IsNullOrEmpty(add))
                {
                    error.Add(add);
                }
            }

            if (error.Count > 0)
            {
                string conteudo = string.Join(", ", error);
                if (error.Count == 1)
                {
                    MessageBox.Show($"The following file:\n{conteudo} is already selected, please remove it and try again", "A file is already selected", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    MessageBox.Show($"The following files:\n{conteudo} are already selected, please remove them and try again", "Some files are already selected", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                ChangeButtonVisibility(true);
				return;
            }
            UpdateStatusText("Files loaded",clear:true);
        }
		else
		{
            ChangeButtonVisibility(true);
        }
    }

    private void Grid_DragOver(object sender, DragEventArgs e)
	{
		string[] array = (string[])e.Data.GetData(DataFormats.FileDrop, autoConvert: false);
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i].Contains(".apk"))
			{
				ApkFilesList.AllowDrop = true;
				continue;
			}
			e.Effects = DragDropEffects.None;
			e.Handled = true;
		}
	}

	private void Grid_Drop(object sender, DragEventArgs e)
	{
        List<string> error = new List<string>();
        string[] array = (string[])e.Data.GetData(DataFormats.FileDrop, autoConvert: false);
		foreach (string filename in array)
        {
            if (filename.Contains(".apk"))
			{
				string add = AddApkFile(filename);
				if (!string.IsNullOrEmpty(add))
                {
                    error.Add(add);
                }
            }
		}

        if (error.Count > 0)
        {
            string conteudo = string.Join(", ", error);
			if (error.Count == 1)
			{
				MessageBox.Show($"The following file:\n{conteudo} is already selected, please remove it and try again", "A file is already selected", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
			else
			{
				MessageBox.Show($"The following files:\n{conteudo} are already selected, please remove them and try again", "Some files are already selected", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }

	private string AddApkFile(string filename)
	{
        string appError = "";
        string fileName = Path.GetFileName(filename);
		StackPanel stackPanel = new StackPanel
		{
			Orientation = Orientation.Horizontal
		};

        foreach (StackPanel item in ApkFilesList.Items)
        {
            foreach (var child in item.Children)
            {
				if (child is TextBlock itemText)
				{
					string text = itemText.Text;
					if (Path.GetFileName(filename) == itemText.Text)
					{
						appError = (Path.GetFileName(filename));
						return appError;
					}
				}
            }
        }
        ChangeButtonVisibility(true);

        TextBlock element = new TextBlock
		{
			Text = fileName,
			Margin = new Thickness(0.0, 0.0, 10.0, 0.0)
		};
		Button button = new Button
		{
			Content = "Delete",
			Tag = filename
		};
		button.Click += DeleteButton_Click;
		stackPanel.Children.Add(element);
		stackPanel.Children.Add(button);
		stackPanel.Tag = filename;
        ApkFilesList.Items.Add(stackPanel);
		return appError;
	}

	private void DeleteButton_Click(object sender, RoutedEventArgs e)
	{
		Button obj = (Button)sender;
		obj.Tag.ToString();
		StackPanel removeItem = (StackPanel)obj.Parent;
		ApkFilesList.Items.Remove(removeItem);
		if (ApkFilesList.Items.Count == 0)
		{
			Install_Button.IsEnabled = false;
		}
	}

    private async void InstallButton_Click(object sender, RoutedEventArgs e)
    {
		string? device = CheckDeviceComboBox();
        if (device == null)
        {
            StatusText.Text = "Please select a device.\n";
            StatusText.Foreground = Brushes.Red;
        }
        else if (Install_Button.Content.ToString() == "Install APKs")
        {
            ApkFilesList.IsEnabled = false;
            Install_Button.Content = "Stop";
            Install_Button.Background = Brushes.Red;
            StatusText.Text = "";
            if (ApkFilesList.Items.Count == 0)
            {
                AdbHelper.Instance.StopCommand();
                StatusText.Text = "Select an APK file to install.\n";
                StatusText.Foreground = Brushes.Red;
                ApkFilesList.IsEnabled = true;
                Install_Button.Content = "Install APKs";
                Install_Button.Background = Brushes.Green;
                return;
            }
            List<string> list = new List<string>();
            foreach (StackPanel item2 in (IEnumerable)ApkFilesList.Items)
            {
                string item = item2.Tag.ToString();
                list.Add(item);
            }
            string deviceSerialByName = GetDeviceSerialByName(device);
            await InstallApks(deviceSerialByName, list);
        }
        else
        {
            loopCancelation = true;
			success = false;
            AdbHelper.Instance.StopCommand();
            UpdateStatusText("Installation canceled");
            StatusText.Foreground = Brushes.Red;
            ApkFilesList.IsEnabled = true;
            Install_Button.Content = "Install APKs";
            Install_Button.Background = Brushes.Green;
            return;
        }
    }

    private async Task InstallApks(string device, List<string> apkFiles)
    {
		string outputResult = "";
        Dispatcher.Invoke(() =>
        {
            StatusText.Text = "Initializing the installation\n";
            StatusText.Foreground = Brushes.White;
        });
        try
        {
            success = true;
			foreach (string apkFile in apkFiles)
			{
				if (loopCancelation == false)
				{
					StatusText.Text += $"\nInstalling \"{apkFile}\"\n";
					StatusText.ScrollToEnd();
					await AdbHelper.Instance.RunAdbCommandAsync($"install -r -d \"{apkFile}\"", device, shell: false, output =>
					{
						base.Dispatcher.Invoke(() =>
						{
							UpdateStatusText(output);
							outputResult += output;
						});
					});

                    if (!outputResult.Contains("Success"))
                    {
						loopCancelation = true;
						success = false;
                    }
                }
				else
                {
					success = false;
                    break;
				}
			}
            Dispatcher.Invoke(() =>
            {
                if (success)
                {
                    StatusText.Text += "Installation complete.\n";
                    StatusText.Foreground = Brushes.Green;
                }
                else
                {
                    StatusText.Text += "Installation not complete.\n";
                    StatusText.Foreground = Brushes.Red;
                }
                StatusText.ScrollToEnd();
            });
            Install_Button.Content = "Install APKs";
            Install_Button.Background = Brushes.Green;
			ApkFilesList.IsEnabled = true;
			loopCancelation = false;
        }
        catch (Exception ex)
        {
            Dispatcher.Invoke(() =>
            {
                StatusText.Text += $"Error: {ex.Message}\n";
                StatusText.Foreground = Brushes.Red;
                StatusText.ScrollToEnd();
            });
        }
    }


    private Dictionary<string, string> GetConnectedDevices()
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		using StringReader stringReader = new StringReader(RunAdbCommand("devices"));
		string text;
		while ((text = stringReader.ReadLine()) != null)
		{
			if (text.EndsWith("device"))
			{
				string text2 = text.Split('\t')[0];
				string deviceName = GetDeviceName(text2);
				dictionary.Add(text2, deviceName);
			}
		}
		return dictionary;
	}

	public string GetDeviceName(string serial)
	{
		return RunAdbCommand("-s " + serial + " shell getprop ro.product.model").Split('\n')[0].Trim();
	}

	public string GetDeviceSerialByName(string name)
	{
		foreach (object item3 in (IEnumerable)DevicesComboBox.Items)
		{
			if (!(item3.ToString() == name))
			{
				continue;
			}
			foreach (object item2 in DevicesComboBox.ItemsSource)
			{
				if (item2.ToString() == name)
				{
					return item2.ToString()!.Split('(')[1].Trim(' ', ')');
				}
			}
		}
		return null!;
	}

	private string RunAdbCommand(string arguments)
	{
		Process process = new Process();
		process.StartInfo.FileName = "adb";
		process.StartInfo.Arguments = arguments;
		process.StartInfo.RedirectStandardOutput = true;
		process.StartInfo.RedirectStandardError = true;
		process.StartInfo.UseShellExecute = false;
		process.StartInfo.CreateNoWindow = true;
		process.Start();
		string text = process.StandardOutput.ReadToEnd();
		string text2 = process.StandardError.ReadToEnd();
		process.WaitForExit();
		return text + text2;
	}

	private void KidsWindow_Click(object sender, RoutedEventArgs e)
    {
        string? device = CheckDeviceComboBox();
        if (device == null)
		{
			MessageBox.Show("Please select a device before opening Kids options.", "No Device Selected", MessageBoxButton.OK, MessageBoxImage.Exclamation);
		}
		else if (kidsWindow == null || !kidsWindow.IsVisible)
		{
			kidsWindow = new Kids(this, GetDeviceSerialByName(device));
			if (base.Left + base.Width + 250 >= SystemParameters.PrimaryScreenWidth || moreWindow != null || settingsWindow != null)
			{
				kidsWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
			}
			else
			{
				kidsWindow.Left = base.Left + base.Width;
				kidsWindow.Top = base.Top;
			}
			Install_Button.IsEnabled = false;
			DevicesComboBox.IsEnabled = false;
			ParentalCare_Button.IsEnabled = false;
			Browse_Button.IsEnabled = false;
			kidsWindow.Show();
            kidsWindow.Closed += KidsWindow_Closed;
		}
		else
		{
			SystemSounds.Exclamation.Play();
			kidsWindow.Activate();
		}
	}

    private void KidsWindow_Closed(object? sender, EventArgs e)
    {
		if (ApkFilesList.Items.Count > 0)
		{
			Install_Button.IsEnabled = true;
		}
		kidsWindow = null;
    }

    private void PCWindow_Click(object sender, RoutedEventArgs e)
    {
        string? device = CheckDeviceComboBox();
        if (device == null)
		{
			MessageBox.Show("Please select a device before opening Parental Care options.", "No Device Selected", MessageBoxButton.OK, MessageBoxImage.Exclamation);
		}
		else if ((settingsWindow == null || !settingsWindow.IsVisible))
		{
			settingsWindow = new Settings(this, GetDeviceSerialByName(device));
            if (base.Left + base.Width + 220 >= SystemParameters.PrimaryScreenWidth || moreWindow != null || kidsWindow != null)
			{
				settingsWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
			}
			else
			{
				settingsWindow.Left = base.Left + base.Width;
				settingsWindow.Top = base.Top;
			}
			ApkFilesList.IsEnabled = false;
			Install_Button.IsEnabled = false;
			DevicesComboBox.IsEnabled = false;
			Kids_Button.IsEnabled = false;
			Browse_Button.IsEnabled = false;
			settingsWindow.Show();
            settingsWindow.Closed += SettingsWindow_Closed;
		}
		else
		{
			SystemSounds.Exclamation.Play();
			settingsWindow.Activate();
		}
	}

    private void SettingsWindow_Closed(object? sender, EventArgs e)
    {
        if (ApkFilesList.Items.Count > 0)
        {
            Install_Button.IsEnabled = true;
        }
        settingsWindow = null;
    }

    public void ActivateDevicesBox()
	{
		DevicesComboBox.IsEnabled = true;
	}

	public void UpdateStatusText(string message, bool isError = false, bool clear = false)
	{
        if (clear == true)
        {
			StatusText.Text = "";
        }
        base.Dispatcher.Invoke(delegate
		{
			TextBox statusText = StatusText;
			statusText.Text = statusText.Text + message + Environment.NewLine;
			StatusText.Foreground = (isError ? Brushes.Red : Brushes.White);
			StatusText.ScrollToEnd();
		});
	}

	public void EmptyOutput_Button(object sender, RoutedEventArgs e)
	{
		ApkFilesList.Items.Clear();
		StatusText.Text = string.Empty;
		StatusText.Foreground = Brushes.White;
	}

    private void More_Button_Click(object sender, RoutedEventArgs e)
    {
		string? device = CheckDeviceComboBox();
        if (device == null)
        {
            MessageBox.Show("Please select a device before opening more options.", "No Device Selected", MessageBoxButton.OK, MessageBoxImage.Exclamation);
        }
        else if (moreWindow == null || !moreWindow.IsVisible)
        {
            moreWindow = new More(this, GetDeviceSerialByName(device), settingsWindow, kidsWindow);
            if (base.Left + base.Width + 250 >= SystemParameters.PrimaryScreenWidth || settingsWindow != null || kidsWindow != null)
            {
                moreWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            }
            else
            {
                moreWindow.Left = base.Left + base.Width;
                moreWindow.Top = base.Top;
            }
			DevicesComboBox.IsEnabled = false;
			moreWindow.Show();
            moreWindow.Closed += MoreWindow_Closed;
		}
		else
		{
            SystemSounds.Exclamation.Play();
            moreWindow.Activate();
        }
    }

    private void MoreWindow_Closed(object? sender, EventArgs e)
    {
        if (settingsWindow == null && kidsWindow == null)
        {
            this.DevicesComboBox.IsEnabled = true;
            this.Activate();
        }
        else
        {
            this.Activate();
        }
        moreWindow = null;
    }

    private void MainWindow_Closing(object? sender, CancelEventArgs e)
    {
#if !DEBUG
		RunAdbCommand("kill-server");
#endif
    }
}
