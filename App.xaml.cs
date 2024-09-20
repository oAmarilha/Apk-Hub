using System.Configuration;
using System.Data;
using System;
using System.IO;
using System.Windows;
using System.Threading.Tasks;

namespace ApkInstaller
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private string logFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "ApkHub\\Log\\Crashes");
        private string fileNamePath = "";
        protected override void OnStartup(StartupEventArgs e)
        {
            fileNamePath = Path.Combine(logFilePath, "crash_log.txt");
            base.OnStartup(e);

            // Registrar manipulação de exceções não tratadas para o AppDomain
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            // Registrar manipulação de exceções não tratadas para exceções na UI thread (WPF)
            DispatcherUnhandledException += App_DispatcherUnhandledException;

            // Registrar manipulação de exceções não tratadas para tarefas em segundo plano
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            // Log de exceções não tratadas no nível de AppDomain
            LogException(e.ExceptionObject as Exception, "AppDomain Unhandled Exception");
            ShowCrashPopup();
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            // Log de exceções não tratadas na UI thread (WPF)
            LogException(e.Exception, "Dispatcher Unhandled Exception");
            e.Handled = true; // Evita o encerramento imediato do aplicativo
            ShowCrashPopup();
        }

        private void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            // Log de exceções não tratadas em tarefas em segundo plano (Task)
            LogException(e.Exception, "Task Unobserved Exception");
            e.SetObserved(); // Marca a exceção como observada para evitar terminação da aplicação
            ShowCrashPopup();
        }

        private void LogException(Exception? ex, string exceptionType)
        {
            Directory.CreateDirectory(logFilePath);
            try
            {
                using (StreamWriter writer = new StreamWriter(fileNamePath, true))
                {
                    writer.WriteLine($"[{DateTime.Now}] {exceptionType}: {ex?.Message}");
                    writer.WriteLine(ex?.StackTrace);
                    writer.WriteLine(new string('-', 80));
                }
            }
            catch (Exception logEx)
            {
                Console.WriteLine($"Erro ao salvar log: {logEx.Message}");
            }
        }

        private void ShowCrashPopup()
        {
            MessageBox.Show($"Ocorreu um erro inesperado. Um log foi salvo em: {fileNamePath}, contacte v.amarilha e compartilhe o arquivo de log para investigação", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

}
