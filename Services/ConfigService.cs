using System.Globalization;
using System.IO;
using CsvHelper;
using CsvHelper.Configuration;
using MABAppTecnologia.Models;
using Newtonsoft.Json;

namespace MABAppTecnologia.Services
{
    public class ConfigService
    {
        private readonly LogService _logService;
        private readonly string _appPath;
        public AppConfig AppConfig { get; private set; }

        public ConfigService(LogService logService)
        {
            _logService = logService;
            _appPath = AppDomain.CurrentDomain.BaseDirectory;
            AppConfig = LoadAppConfig();
        }

        private AppConfig LoadAppConfig()
        {
            var configPath = Path.Combine(_appPath, "Config", "settings.json");

            if (File.Exists(configPath))
            {
                try
                {
                    var json = File.ReadAllText(configPath);
                    return JsonConvert.DeserializeObject<AppConfig>(json) ?? new AppConfig();
                }
                catch (Exception ex)
                {
                    _logService.LogWarning($"No se pudo cargar settings.json, usando configuración por defecto. Error: {ex.Message}");
                }
            }

            return new AppConfig();
        }

        public List<ConsorcioConfig> LoadConsorcios()
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
