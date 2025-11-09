using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using MABAppTecnologia.Services;
using MABAppTecnologia.ViewModels;
using MABAppTecnologia.Models;
using System.IO;

namespace MABAppTecnologia
{
    public static class ServiceConfiguration
    {
        public static IServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();

            // ===== CONFIGURACIÓN =====
            // Cargar appsettings.json usando IConfiguration
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile("Config/settings.json", optional: true, reloadOnChange: true) // Backward compatibility
                .Build();

            services.AddSingleton<IConfiguration>(configuration);

            // Configurar IOptions<AppConfig> con validación
            services.AddOptions<AppConfig>()
                .Bind(configuration.GetSection("AppConfig"))
                .ValidateDataAnnotations() // Valida usando DataAnnotations
                .ValidateOnStart();        // Valida al iniciar la aplicación (fail-fast)

            // ===== SERVICIOS =====
            // Singleton: Una única instancia compartida durante toda la aplicación
            // Se usa para servicios sin estado que son thread-safe

            // Logging con Serilog (Structured Logging)
            // Registrar como Singleton para mantener una única instancia
            var structuredLogService = new StructuredLogService();
            services.AddSingleton<ILogService>(structuredLogService);
            services.AddSingleton<IStructuredLogService>(structuredLogService);

            // Memory Cache (para caching en memoria)
            services.AddMemoryCache();
            services.AddSingleton<ICacheService, CacheService>();

            services.AddSingleton<IConfigService, ConfigService>();

            // Transient: Nueva instancia cada vez que se solicita
            // Se usa para servicios ligeros y stateless
            services.AddTransient<ISystemService, SystemService>();
            services.AddTransient<IUserService, UserService>();
            services.AddTransient<IPersonalizationService, PersonalizationService>();
            services.AddTransient<ISoftwareService, SoftwareService>();

            // ===== VIEW MODELS =====
            // Transient para ViewModels permite crear nuevas instancias limpias
            services.AddTransient<MainViewModel>();

            // ===== WINDOWS =====
            // Transient para Windows permite múltiples instancias si fuera necesario
            services.AddTransient<MainWindow>();

            return services.BuildServiceProvider();
        }
    }
}
