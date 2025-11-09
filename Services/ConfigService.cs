using System.Globalization;
using System.IO;
using CsvHelper;
using CsvHelper.Configuration;
using MABAppTecnologia.Models;
using Microsoft.Extensions.Options;

namespace MABAppTecnologia.Services
{
    public class ConfigService : IConfigService
    {
        private readonly ILogService _logService;
        private readonly ICacheService? _cacheService;
        private readonly string _appPath;
        private readonly IOptionsMonitor<AppConfig> _appConfigMonitor;
        private const string CONSORCIOS_CACHE_KEY = "consorcios_list";
        private static readonly TimeSpan CONSORCIOS_CACHE_DURATION = TimeSpan.FromMinutes(30);

        /// <summary>
        /// Configuración de la aplicación (inyectada y validada mediante IOptions)
        /// </summary>
        public AppConfig AppConfig => _appConfigMonitor.CurrentValue;

        public ConfigService(ILogService logService, IOptionsMonitor<AppConfig> appConfigMonitor, ICacheService? cacheService = null)
        {
            _logService = logService;
            _appConfigMonitor = appConfigMonitor;
            _cacheService = cacheService;
            _appPath = AppDomain.CurrentDomain.BaseDirectory;

            // Log de configuración cargada y validada
            if (_logService is IStructuredLogService structuredLog)
            {
                structuredLog.LogInformation(
                    "Configuración cargada y validada: ResourcesPath={ResourcesPath}, WallpapersFolder={WallpapersFolder}, ConsorciosCSV={ConsorciosCSV}",
                    AppConfig.MABResourcesPath,
                    AppConfig.WallpapersFolder,
                    AppConfig.ConsorciosCSVPath);
            }
            else
            {
                _logService.LogInfo($"Configuración cargada desde IOptions: {AppConfig.MABResourcesPath}");
            }
        }

        public List<ConsorcioConfig> LoadConsorcios()
        {
            // Intentar obtener del caché si está disponible
            if (_cacheService != null)
            {
                return _cacheService.GetOrCreate(CONSORCIOS_CACHE_KEY, () => LoadConsorciosFromFile(), CONSORCIOS_CACHE_DURATION);
            }

            // Si no hay caché, cargar directamente del archivo
            return LoadConsorciosFromFile();
        }

        private List<ConsorcioConfig> LoadConsorciosFromFile()
        {
            var csvPath = Path.Combine(_appPath, AppConfig.ConsorciosCSVPath);

            _logService.LogInfo($"Intentando cargar CSV desde: {csvPath}");

            if (!File.Exists(csvPath))
            {
                _logService.LogError($"No se encontró el archivo CSV de consorcios en: {csvPath}");
                return new List<ConsorcioConfig>();
            }

            try
            {
                var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HasHeaderRecord = true,
                    MissingFieldFound = null,
                    TrimOptions = CsvHelper.Configuration.TrimOptions.Trim,
                    BadDataFound = null
                };

                // Usar UTF-8 con detección de BOM
                using var reader = new StreamReader(csvPath, System.Text.Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
                using var csv = new CsvReader(reader, config);

                var records = csv.GetRecords<ConsorcioConfig>().ToList();

                _logService.LogSuccess($"Se cargaron {records.Count} consorcios desde CSV");

                // Log de cada consorcio cargado
                foreach (var consorcio in records)
                {
                    _logService.LogInfo($"Consorcio cargado: {consorcio.Consorcio} ({consorcio.Siglas})");
                }

                return records;
            }
            catch (Exception ex)
            {
                _logService.LogError($"Error al leer el archivo CSV de consorcios", ex);
                return new List<ConsorcioConfig>();
            }
        }

        public void EnsureResourcesInSystemFolder()
        {
            try
            {
                // Crear carpeta principal en C:
                Directory.CreateDirectory(AppConfig.MABResourcesPath);

                // Crear subcarpetas
                Directory.CreateDirectory(Path.Combine(AppConfig.MABResourcesPath, AppConfig.WallpapersFolder));
                Directory.CreateDirectory(Path.Combine(AppConfig.MABResourcesPath, AppConfig.ProfileImagesFolder));
                Directory.CreateDirectory(Path.Combine(AppConfig.MABResourcesPath, AppConfig.LogsFolder));

                // Copiar recursos desde la app hacia C:\MAB-Resources
                CopyResourcesFolder("Resources\\Wallpapers", Path.Combine(AppConfig.MABResourcesPath, AppConfig.WallpapersFolder));
                CopyResourcesFolder("Resources\\ProfileImages", Path.Combine(AppConfig.MABResourcesPath, AppConfig.ProfileImagesFolder));

                _logService.LogSuccess($"Recursos copiados exitosamente a {AppConfig.MABResourcesPath}");
            }
            catch (Exception ex)
            {
                _logService.LogError("Error al copiar recursos al sistema", ex);
                throw;
            }
        }

        private void CopyResourcesFolder(string sourceRelativePath, string destinationPath)
        {
            var sourcePath = Path.Combine(_appPath, sourceRelativePath);

            if (!Directory.Exists(sourcePath))
            {
                _logService.LogWarning($"No se encontró la carpeta de origen: {sourcePath}");
                return;
            }

            foreach (var file in Directory.GetFiles(sourcePath))
            {
                var fileName = Path.GetFileName(file);
                var destFile = Path.Combine(destinationPath, fileName);
                File.Copy(file, destFile, true);
            }
        }
    }
}
