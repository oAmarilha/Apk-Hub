using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;

namespace ApkInstaller.Helper_classes
{
    public class AdbHelper
    {
        private static AdbHelper? instance;
        private List<Process> processes;
        public string appPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}\\ApkHub\\Log";
        private string adbExecutablePath;
        private string scrcpyExecutablePath;
        private string aaptExecutablePath;
        string? androidHome;
        MainWindow mainWindow = MainWindow.Instance;

        private AdbHelper()
        {
            processes = new List<Process>();
            androidHome = Environment.GetEnvironmentVariable("ANDROID_HOME");
            string localPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "adb");
            aaptExecutablePath = Path.Combine(localPath, "aapt.exe");
            if (AreExecutablesInAndroidHome())
            {
                adbExecutablePath = Path.Combine(androidHome!, "platform-tools", "adb.exe");
                scrcpyExecutablePath = Path.Combine(androidHome!, "platform-tools", "scrcpy.exe");
            }
            else
            {
                // Caso contrário, usa os executáveis da pasta local (adb/adb.exe e adb/scrcpy.exe)
                adbExecutablePath = Path.Combine(localPath, "adb.exe");
                scrcpyExecutablePath = Path.Combine(localPath, "scrcpy.exe");
            }
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

        private bool AreExecutablesInAndroidHome()
        {
            if (!string.IsNullOrEmpty(androidHome))
            {
                string adbPath = Path.Combine(androidHome, "platform-tools", "adb.exe");
                string scrcpyPath = Path.Combine(androidHome, "platform-tools", "scrcpy.exe");

                // Retorna true se ambos os arquivos existirem
                return File.Exists(adbPath) && File.Exists(scrcpyPath);
            }
            return false;
        }

        public async Task RunAdbCommandAsync(string arguments, Action<string> outputHandler, string? selectedDevice = null, bool shell = false, bool generalCommand = false)
        {
            await Task.Run(() =>
            {
                try
                {
                    Process process = new Process();
                    process.StartInfo.FileName = adbExecutablePath;
                    process.StartInfo.Arguments = generalCommand ? $" {arguments}" : (shell ? "-s " + selectedDevice + " shell " + arguments : "-s " + selectedDevice + $" {arguments}");
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardError = true;
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.CreateNoWindow = true;

                    process.OutputDataReceived += (sender, e) =>
                    {
                        outputHandler(e.Data);  // Notify UI with the output
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
                        outputHandler(e.Data);  // Notify UI with the output
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
                    outputHandler("Error executing command: " + ex2.Message);  // Notify UI with the error
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

        public async Task StartScreenRecording(string selectedDevice, string localFile)
        {
            string fileName = DateTime.Now.ToString().Replace("/", "-").Replace(":", "-")
                .Replace(" ", "_") ?? "";
            string deviceName = await mainWindow.GetDeviceName(selectedDevice);
            string remotefile = $"{deviceName}_{fileName}";
            try
            {
                await RunAdbCommandAsync($"screenrecord /sdcard/{deviceName}_{fileName}.mp4", output => { }, selectedDevice, true);
            }
            catch (Exception)
            {
                MessageBox.Show("Error during screen recording, try again", "Error", MessageBoxButton.OK, MessageBoxImage.Hand);
                return;
            }

            await Task.Delay(1500);
            TransferRecordedFile(remotefile, deviceName, selectedDevice);

        }

        public async void TransferRecordedFile(string remoteFile, string deviceName, string selectedDevice)
        {
            string directoryPath = $"{appPath}\\ScreenRecords\\{deviceName}\\{remoteFile}";
            string fullPath = Path.Combine(directoryPath, remoteFile + ".mp4");
            string fileName = remoteFile + ".mp4";

            try
            {
                Directory.CreateDirectory(directoryPath);
                await RunAdbCommandAsync($"pull /sdcard/{fileName} \"{fullPath}\"", output =>
                {
                    mainWindow.UpdateStatusText(output);
                }, selectedDevice);
                await Task.Delay(500);
                MessageBoxResult result = MessageBox.Show($"Screen recording saved to: {fullPath}\nDo you want to open it?", "Success", MessageBoxButton.YesNo, MessageBoxImage.Asterisk);
                if (result == MessageBoxResult.Yes)
                {
                    Process.Start(new ProcessStartInfo()
                    {
                        FileName = directoryPath,
                        UseShellExecute = true,
                        Verb = "open"
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error transferring recorded file: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Hand);
            }
        }

        public async Task RealTimeScreen(string selectedDevice)
        {
            await AdbHelper.Instance.RunCommandAsync($"{scrcpyExecutablePath}", $"-s {selectedDevice}", output =>
            {
                mainWindow.UpdateStatusText(output);
            });
        }

        private void EndRealTimeScreen()
        {
            AdbHelper.Instance.StopCommand();
        }

        public async Task<bool> UninstallFunction(string _selectedDevice, string appPkg)
        {
            mainWindow.StatusText.Foreground = Brushes.White;
            string result = "";
            string appName = await GetAppName(appPkg, _selectedDevice);
            await RunAdbCommandAsync($"uninstall {appPkg}", output =>
            {
#if DEBUG
                mainWindow.UpdateStatusText(output);
#endif
                result += output;
            }, _selectedDevice);
            if (result.Contains("Success"))
            {
                appName = Encoding.UTF8.GetString(Encoding.GetEncoding("ISO-8859-1").GetBytes(appName));
                mainWindow.UpdateStatusText($"The app {appName} was succesfully uninstalled");
                mainWindow.StatusText.Foreground = Brushes.Green;
                Directory.Delete($"{appPath}/apk", true);
                return true;
            }
            else
            {
                mainWindow.UpdateStatusText($"The app {appPkg} was not uninstalled, check the package's name and try again");
                mainWindow.StatusText.Foreground = Brushes.Red;
                Directory.Delete($"{appPath}/apk", true);
                return false;
            }
        }

        public async Task ClearAppFunction(string _selectedDevice, string appPkg)
        {
            mainWindow.StatusText.Foreground = Brushes.White;
            string result = "";
            string appName = await GetAppName(appPkg, _selectedDevice);
            await RunAdbCommandAsync($"pm clear {appPkg}", output =>
            {
                mainWindow.UpdateStatusText(output);
                result += output;
            }, _selectedDevice, true);
            if (result.Contains("Success"))
            {
                appName = Encoding.UTF8.GetString(Encoding.GetEncoding("ISO-8859-1").GetBytes(appName));
                mainWindow.UpdateStatusText($"The app {appName} was succesfully cleared");
                mainWindow.StatusText.Foreground = Brushes.Green;
            }
            else
            {
                mainWindow.UpdateStatusText($"The app {appPkg} was not cleared, check the package's name and try again");
                mainWindow.StatusText.Foreground = Brushes.Red;
            }
            Directory.Delete($"{appPath}/apk", true);
        }

        public async Task<string> GetAppName(string appPkg, string _selectedDevice)
        {
            Directory.CreateDirectory($"{appPath}/apk");
            string apkFilePath = "";
            string apkName = "";
            string appName = "";
            string outputCommand = "";
            await RunAdbCommandAsync($"pm list package -f {appPkg}", output =>
            {
                apkFilePath += output;
            }, _selectedDevice, true);
            appName = RegexFunction($@"package:(/data/app/~~.*?)/base\.apk={appPkg}", apkFilePath);
            if (!string.IsNullOrEmpty(appName))
            {
                apkName = appName + "/base.apk";
                await RunAdbCommandAsync($"pull {apkName} {appPath}/apk/base.apk", output => { }, _selectedDevice, false);
                await RunCommandAsync($"{aaptExecutablePath}", $"d badging {appPath}/apk/base.apk", output => { outputCommand += output; });

                appName = RegexFunction(@"application-label-en(?:-[a-zA-Z]{2})*:'(.*?)'(?:application-label|$)", Encoding.UTF8.GetString(Encoding.GetEncoding("ISO-8859-1").GetBytes(outputCommand)));
            }
            return appName;
        }

        public string RegexFunction(string expression, string file)
        {
            string find = "";
            Regex name = new Regex(expression);
            Match findName = name.Match(file);
            if (findName.Success)
            {
                find = findName.Groups[1].Value.Replace("\\n", " ");
            }
            return find;
        }
    }
}
