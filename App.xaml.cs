using System.Configuration;
using System.Data;
using System.Windows;
using System.IO;
using Microsoft.Extensions.DependencyInjection;

namespace MABAppTecnologia;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private IServiceProvider? _serviceProvider;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Configurar Dependency Injection
        _serviceProvider = ServiceConfiguration.ConfigureServices();

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

        // Crear y mostrar la ventana principal usando DI
        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }
}

