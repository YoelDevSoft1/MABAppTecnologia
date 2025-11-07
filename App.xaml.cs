using System.Configuration;
using System.Data;
using System.Windows;
using System.IO;

namespace MABAppTecnologia;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        // Capturar excepciones no manejadas
        AppDomain.CurrentDomain.UnhandledException += (s, args) =>
        {
            var exception = args.ExceptionObject as Exception;
            var errorMessage = $"Error no manejado:\n{exception?.Message}\n\nStack Trace:\n{exception?.StackTrace}";
            
            try
            {
                var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash_log.txt");
                File.WriteAllText(logPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\n{errorMessage}");
            }
            catch { }
            
            MessageBox.Show(errorMessage, "Error Fatal", MessageBoxButton.OK, MessageBoxImage.Error);
        };
        
        DispatcherUnhandledException += (s, args) =>
        {
            var errorMessage = $"Error en UI:\n{args.Exception.Message}\n\nStack Trace:\n{args.Exception.StackTrace}";
            
            try
            {
                var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash_log.txt");
                File.WriteAllText(logPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\n{errorMessage}");
            }
            catch { }
            
            MessageBox.Show(errorMessage, "Error en Interfaz", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };
    }
}

