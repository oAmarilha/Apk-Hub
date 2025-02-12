using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using ApkInstaller.Helper_classes;

namespace ApkInstaller.Feature_classes
{
    /// <summary>
    /// Interaction logic for FileExplorerWindow.xaml
    /// </summary>
    public partial class FileExplorerWindow : Window
    {
        private string _currentDevice;
        private const string ROOT_PATH = "sdcard/";
        private string _currentPath = ROOT_PATH;
        private Stack<string> _navigationHistory = new Stack<string>();

        public class FileItem
        {
            public string Name { get; set; }
            public bool IsDirectory { get; set; }
            public string FullPath { get; set; }
            public BitmapSource PreviewSource { get; set; }
        }

        public FileExplorerWindow(string deviceSerial)
        {
            InitializeComponent();
            _currentDevice = deviceSerial;
            LoadDirectory(_currentPath);
        }

        private void FileListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedItem = FileListView.SelectedItem as FileItem;

            // Enable/disable buttons based on selection
            DownloadButton.IsEnabled = selectedItem != null;
            DeleteButton.IsEnabled = selectedItem != null;
        }

        private async void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = FileListView.SelectedItem as FileItem;
            if (selectedItem == null) return;

            try
            {
                // Get the extension, default to empty string if null
                string fileExtension = Path.GetExtension(selectedItem.Name) ?? "";

                // Prepare save file dialog
                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    FileName = selectedItem.Name,
                    DefaultExt = fileExtension,
                    // Fallback to all files if no extension
                    Filter = !string.IsNullOrEmpty(fileExtension)
                        ? $"{fileExtension.TrimStart('.')} files (*{fileExtension})|*{fileExtension}"
                        : "All files (*.*)|*.*"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    // Use ADB to pull the file
                    string pullCommand = $"pull \"{selectedItem.FullPath}\" \"{saveFileDialog.FileName}\"";
                    string result = await AdbHelper.Instance.GetAdbReturn(pullCommand, _currentDevice);

                    if (result.Contains("1 file pulled"))
                    {
                        StatusTextBlock.Text = $"File downloaded to {saveFileDialog.FileName}";

                        // Optional: Open the downloaded file's location
                        Process.Start("explorer.exe", $"/select,\"{saveFileDialog.FileName}\"");
                    }
                    else
                    {
                        StatusTextBlock.Text = "Download failed";
                        MessageBox.Show("Failed to download the file.", "Download Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error downloading file: {ex.Message}", "Download Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = FileListView.SelectedItem as FileItem;
            if (selectedItem == null) return;

            var result = MessageBox.Show($"Are you sure you want to delete {selectedItem.Name}?",
                "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    string deleteCommand = $"rm -rf \"{selectedItem.FullPath}\"";
                    await AdbHelper.Instance.GetAdbReturn(deleteCommand, _currentDevice, true);

                    // Refresh directory
                    LoadDirectory(_currentPath);
                    StatusTextBlock.Text = $"Deleted {selectedItem.Name}";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error deleting file: {ex.Message}", "Delete Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void NewFolderButton_Click(object sender, RoutedEventArgs e)
        {
            // Prompt for folder name
            string folderName = Microsoft.VisualBasic.Interaction.InputBox(
                "Enter new folder name:", "New Folder", "New Folder");

            if (!string.IsNullOrWhiteSpace(folderName))
            {
                try
                {
                    string newFolderPath = Path.Combine(_currentPath, folderName).Replace("\\", "/");
                    string mkdirCommand = $"mkdir -p \"{newFolderPath}\"";
                    await AdbHelper.Instance.GetAdbReturn(mkdirCommand, _currentDevice, true);

                    // Refresh directory
                    LoadDirectory(_currentPath);
                    StatusTextBlock.Text = $"Created folder {folderName}";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error creating folder: {ex.Message}", "New Folder Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            // Clear navigation history and go back to root
            _navigationHistory.Clear();
            _currentPath = ROOT_PATH;
            LoadDirectory(_currentPath);
        }
        private async void LoadDirectory(string path)
        {
            try
            {
                // Run ADB shell command to list directory contents
                string lsCommand = $"ls -la \"{path}\"";
                string result = await AdbHelper.Instance.GetAdbReturn(lsCommand, _currentDevice, true);

                // Log the raw output for debugging
                Debug.WriteLine($"ADB LS Output for {path}:");
                Debug.WriteLine(result);

                // Parse and display
                var fileItems = ParseLsOutput(result, path);

                if (fileItems.Count == 0)
                {
                    // Create a placeholder item to show empty directory
                    fileItems.Add(new FileItem
                    {
                        Name = "This directory is empty",
                        IsDirectory = false,
                        FullPath = path
                    });
                }

                FileListView.ItemsSource = fileItems;

                // Update status text
                StatusTextBlock.Text = fileItems.Count == 1 && fileItems[0].Name == "This directory is empty"
                    ? "Directory is empty"
                    : $"Showing {fileItems.Count} item(s) in {path}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading directory: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                StatusTextBlock.Text = "Error loading directory";
            }
        }
        private List<FileItem> ParseLsOutput(string lsOutput, string currentPath)
        {
            var fileItems = new List<FileItem>();

            // Regex to parse ls -la output
            string pattern = @"^([\-d][\w\-]{9})\s+\d+\s+\w+\s+\w+\s+\d+\s+(\d{4}\-\d{2}\-\d{2}\s+\d{2}:\d{2})\s+(.+)$";

            var lines = lsOutput.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines.Skip(1)) // Skip total blocks line
            {
                var match = Regex.Match(line, pattern);
                if (match.Success)
                {
                    string permissions = match.Groups[1].Value;
                    string fileName = match.Groups[3].Value.Trim();

                    // Skip . and .. directories
                    if (fileName == "." || fileName == "..") continue;

                    fileItems.Add(new FileItem
                    {
                        Name = fileName,
                        IsDirectory = permissions.StartsWith("d"),
                        FullPath = Path.Combine(currentPath, fileName).Replace("\\", "/")
                    });
                }
            }

            return fileItems;
        }

        private void FileListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var selectedItem = FileListView.SelectedItem as FileItem;
            if (selectedItem == null) return;

            if (selectedItem.IsDirectory)
            {
                // Navega para o diretório
                _navigationHistory.Push(_currentPath);
                _currentPath = selectedItem.FullPath;
                LoadDirectory(_currentPath);
            }
            else
            {
                // Verifica se é uma imagem
                string[] imageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };
                string fileExtension = Path.GetExtension(selectedItem.Name).ToLower();

                if (imageExtensions.Contains(fileExtension))
                {
                    // Abre preview de imagem
                    ShowImagePreview(selectedItem.FullPath);
                }
                else
                {
                    // Comportamento padrão para outros tipos de arquivo
                    MessageBox.Show($"Selected file: {selectedItem.Name}", "File Selected");
                }
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (_navigationHistory.Count > 0)
            {
                _currentPath = _navigationHistory.Pop();
                LoadDirectory(_currentPath);
            }
        }

        private void ShowImagePreview(string imagePath)
        {
            // Cria uma janela de preview
            var previewWindow = new Window
            {
                Title = "Image Preview",
                Width = 800,
                Height = 600,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this
            };

            // Cria um grid para organizar o conteúdo
            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Imagem de preview
            var image = new Image
            {
                Stretch = Stretch.Uniform,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            // Informações do arquivo
            var fileInfoPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(10)
            };

            // Carrega a imagem de forma assíncrona
            Task.Run(() =>
            {
                try
                {
                    // Baixa temporariamente a imagem
                    string tempImagePath = Path.Combine(Path.GetTempPath(), Path.GetFileName(imagePath));
                    string pullCommand = $"pull \"{imagePath}\" \"{tempImagePath}\"";
                    string result = AdbHelper.Instance.GetAdbReturn(pullCommand, _currentDevice).Result;

                    // Atualiza a UI com a imagem
                    Dispatcher.Invoke(() =>
                    {
                        if (File.Exists(tempImagePath))
                        {
                            var bitmap = new BitmapImage(new Uri(tempImagePath, UriKind.Absolute));
                            image.Source = bitmap;

                            // Adiciona informações do arquivo
                            var fileInfo = new FileInfo(tempImagePath);
                            var infoTextBlock = new TextBlock
                            {
                                Text = $"Size: {FormatFileSize(fileInfo.Length)} | " +
                                       $"Dimensions: {bitmap.PixelWidth}x{bitmap.PixelHeight} | " +
                                       $"Modified: {fileInfo.LastWriteTime}",
                                Margin = new Thickness(10, 0, 0, 0)
                            };

                            fileInfoPanel.Children.Add(infoTextBlock);
                        }
                    });
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show($"Error previewing image: {ex.Message}", "Preview Error");
                    });
                }
            });

            // Botão de download
            var downloadButton = new Button
            {
                Content = "Download",
                Margin = new Thickness(10)
            };
            downloadButton.Click += (s, e) =>
            {
                // Reutiliza a lógica de download existente
                DownloadButton_Click(s, e);
                previewWindow.Close();
            };

            // Adiciona elementos ao grid
            grid.Children.Add(image);
            Grid.SetRow(image, 0);

            grid.Children.Add(fileInfoPanel);
            Grid.SetRow(fileInfoPanel, 1);

            grid.Children.Add(downloadButton);
            Grid.SetRow(downloadButton, 2);

            previewWindow.Content = grid;
            previewWindow.ShowDialog();
        }

        // Método auxiliar para formatar tamanho do arquivo
        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            int order = 0;
            double len = bytes;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
    }
}
