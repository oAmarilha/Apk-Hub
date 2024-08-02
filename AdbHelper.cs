using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace ApkInstaller
{
    public class AdbHelper
    {
        private static AdbHelper instance;
        private List<Process> processes;

        private AdbHelper()
        {
            processes = new List<Process>();
        }

        public static AdbHelper Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new AdbHelper();
                }
                return instance;
            }
        }

        public async Task RunAdbCommandAsync(string arguments, string selectedDevice, bool shell, Action<string> outputHandler, string command = "adb")
        {
            await Task.Run(() =>
            {
                try
                {
                    Process process = new Process();
                    process.StartInfo.FileName = command;
                    process.StartInfo.Arguments = (shell ? ("-s " + selectedDevice + " shell " + arguments) : ("-s " + selectedDevice + " " + arguments));
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardError = true;
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.CreateNoWindow = true;

                    process.OutputDataReceived += (sender, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            outputHandler(e.Data);  // Notify UI with the output
                        }
                    };

                    process.ErrorDataReceived += (sender, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            outputHandler(e.Data);  // Notify UI with the error
                        }
                    };

                    lock (processes)
                    {
                        processes.Add(process);
                    }

                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                    process.WaitForExit();

                    lock (processes)
                    {
                        processes.Remove(process);
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex2)
                {
                    outputHandler("Error executing adb command: " + ex2.Message);  // Notify UI with the error
                }
            });
            return;
        }



        public async Task StartScreenRecording(MainWindow mainWindow, string selectedDevice, string localFile)
        {
            string fileName = DateTime.Now.ToString().Replace("/", "-").Replace(":", "-")
                .Replace(" ", "_") ?? "";
            string deviceName = mainWindow.GetDeviceName(selectedDevice);
            string remotefile = $"{deviceName}_{fileName}";
            try
            {

                await RunAdbCommandAsync($"shell screenrecord /sdcard/{deviceName}_{fileName}.mp4", selectedDevice, false, output =>
                {
                    
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error during screen recording, try again", "Error", MessageBoxButton.OK, MessageBoxImage.Hand);
                return;
            }
            TransferRecordedFile(mainWindow, remotefile, deviceName, selectedDevice);

        }

        public async void TransferRecordedFile(MainWindow mainWindow, string remoteFile, string deviceName, string selectedDevice)
        {
            string directoryPath = Path.Combine(Directory.GetCurrentDirectory(), "log\\ScreenRecords", deviceName);
            string fullPath = Path.Combine(directoryPath, remoteFile + ".mp4");
            string fileName =  remoteFile + ".mp4";

            try
            {
                Directory.CreateDirectory(directoryPath);
                await RunAdbCommandAsync($"pull /sdcard/{fileName} \"{fullPath}\"", selectedDevice, shell: false, output =>
                {
                    mainWindow.UpdateStatusText(output);
                });
                MessageBox.Show($"Screen recording saved to: {fullPath}", "Success", MessageBoxButton.OK, MessageBoxImage.Asterisk);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error transferring recorded file: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Hand);
            }
        }


        public async Task RunCommandAsync(string command, string arguments, Action<string> outputHandler)
        {
            await Task.Run(() =>
            {
                try
                {
                    Process process = new Process();
                    process.StartInfo.FileName = command;
                    process.StartInfo.Arguments = arguments;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardError = true;
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.CreateNoWindow = true;
                    process.OutputDataReceived += (sender, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            outputHandler(e.Data);
                        }
                    };
                    process.ErrorDataReceived += (sender, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            outputHandler(e.Data);
                        }
                    };
                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                    process.WaitForExit();
                }
                catch (Exception ex)
                {
                    outputHandler("Error executing command: " + ex.Message);
                }
            });
        }

        public void StopCommand()
        {
            lock (processes)
            {
                foreach (var process in processes)
                {
                    if (!process.HasExited)
                    {
                        process.Kill();
                    }
                    process.Dispose();
                }
                processes.Clear();
            }
        }

        public async void UninstallFunction(MainWindow _mainWindow, string _selectedDevice,string appPkg)
        {
            _mainWindow.StatusText.Foreground = Brushes.White;
            string result = "";
            string apkFilePath = "";
            string apkName = "";
            string appName = "";
            string outputCommand = "";
            await AdbHelper.Instance.RunAdbCommandAsync($"pm list package -f {appPkg}", _selectedDevice, true, output =>
            {
                apkFilePath += output;
            });
            appName = AdbHelper.Instance.RegexFunction($@"package:(/data/app/~~.*?)/base\.apk={appPkg}", apkFilePath);
            if (!string.IsNullOrEmpty(appName))
            {
                Directory.CreateDirectory("log/apk");
                apkName = appName + "/base.apk";
                await AdbHelper.Instance.RunAdbCommandAsync($"pull {apkName} log/apk/test.apk", _selectedDevice, false, output => { });
                await AdbHelper.Instance.RunCommandAsync("aapt", $"d badging log/apk/test.apk", output => { outputCommand += output; });

                appName = AdbHelper.Instance.RegexFunction(@"application-label-pt-BR:'([^']*)'", outputCommand);

                Directory.Delete("log/apk", true);
            }
            await AdbHelper.Instance.RunAdbCommandAsync($"uninstall {appPkg}", _selectedDevice, false, output =>
            {
                _mainWindow.UpdateStatusText(output);
                result += output;
            });
            if (result.Contains("Success"))
            {
                appName = Encoding.UTF8.GetString(Encoding.GetEncoding("ISO-8859-1").GetBytes(appName));
                _mainWindow.UpdateStatusText($"The app {appName} was succesfully uninstalled");
                _mainWindow.StatusText.Foreground = Brushes.Green;
            }
            else
            {
                _mainWindow.UpdateStatusText($"The app {appPkg} was not uninstalled, check the package's name and try again");
                _mainWindow.StatusText.Foreground = Brushes.Red;
            }
        }

        public string RegexFunction(string expression, string file)
        {
            string find = "";
            Regex name = new Regex(expression);
            Match findName = name.Match(file);
            if (findName.Success)
            {
                find = findName.Groups[1].Value;
            }
            return find;
        }
    }
}
