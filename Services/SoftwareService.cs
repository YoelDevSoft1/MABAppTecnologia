using System.Diagnostics;
using System.IO;
using MABAppTecnologia.Models;

namespace MABAppTecnologia.Services
{
    public class SoftwareService : ISoftwareService
    {
        private readonly ILogService _logService;
        private readonly string _softwarePath;

        public SoftwareService(ILogService logService)
        {
            _logService = logService;
            var appPath = AppDomain.CurrentDomain.BaseDirectory;
            _softwarePath = Path.Combine(appPath, "Software");
        }

        public List<SoftwareItem> GetAvailableSoftware()
        {
            var softwareList = new List<SoftwareItem>();

            if (!Directory.Exists(_softwarePath))
            {
                _logService.LogWarning($"No se encontró la carpeta de software: {_softwarePath}");
                Directory.CreateDirectory(_softwarePath);
                return softwareList;
            }

            var extensions = new[] { "*.exe", "*.msi" };

            // Buscar en carpeta raíz
            foreach (var extension in extensions)
            {
                var files = Directory.GetFiles(_softwarePath, extension, SearchOption.TopDirectoryOnly);
                foreach (var file in files)
                {
                    softwareList.Add(new SoftwareItem
                    {
                        Name = Path.GetFileNameWithoutExtension(file),
                        FileName = Path.GetFileName(file),
                        FullPath = file,
                        Category = "General", // Sin categoría (raíz)
                        IsSelected = false,
                        IsInstalling = false,
                        IsInstalled = false,
                        Status = "Pendiente"
                    });
                }
            }

            // Buscar en subcarpetas (recursivo)
            try
            {
                var subdirectories = Directory.GetDirectories(_softwarePath);
                foreach (var subdirectory in subdirectories)
                {
                    var categoryName = Path.GetFileName(subdirectory);

                    foreach (var extension in extensions)
                    {
                        var files = Directory.GetFiles(subdirectory, extension, SearchOption.AllDirectories);
                        foreach (var file in files)
                        {
                            // Obtener la ruta relativa para mostrar subcategorías
                            var relativePath = Path.GetRelativePath(_softwarePath, Path.GetDirectoryName(file) ?? "");
                            var category = relativePath.Replace("\\", " > "); // Muestra jerarquía

                            softwareList.Add(new SoftwareItem
                            {
                                Name = Path.GetFileNameWithoutExtension(file),
                                FileName = Path.GetFileName(file),
                                FullPath = file,
                                Category = category,
                                IsSelected = false,
                                IsInstalling = false,
                                IsInstalled = false,
                                Status = "Pendiente"
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logService.LogWarning($"Error al buscar en subcarpetas: {ex.Message}");
            }

            // Ordenar por categoría y luego por nombre
            softwareList = softwareList.OrderBy(s => s.Category).ThenBy(s => s.Name).ToList();

            _logService.LogInfo($"Se encontraron {softwareList.Count} aplicaciones para instalar");

            // Log de categorías encontradas
            var categories = softwareList.Select(s => s.Category).Distinct().ToList();
            _logService.LogInfo($"Categorías detectadas: {string.Join(", ", categories)}");

            return softwareList;
        }

        public async Task<OperationResult> InstallSoftware(SoftwareItem software, IProgress<string>? progress = null)
        {
            try
            {
                _logService.LogInfo($"Iniciando instalación de: {software.Name}");
                progress?.Report($"Instalando {software.Name}...");

                var extension = Path.GetExtension(software.FullPath).ToLower();

                // Intentar instalación silenciosa primero
                var result = extension switch
                {
                    ".msi" => await InstallMsi(software, true),
                    ".exe" => await InstallExe(software, true),
                    _ => OperationResult.Fail($"Formato no soportado: {extension}")
                };

                // Si la instalación silenciosa falla, intentar interactiva
                if (!result.Success && result.Details.Contains("silent"))
                {
                    _logService.LogWarning($"Instalación silenciosa falló para {software.Name}, intentando modo interactivo");
                    progress?.Report($"Instalando {software.Name} (modo interactivo)...");

                    result = extension switch
                    {
                        ".msi" => await InstallMsi(software, false),
                        ".exe" => await InstallExe(software, false),
                        _ => result
                    };
                }

                if (result.Success)
                {
                    _logService.LogSuccess($"Instalación completada: {software.Name}");
                    progress?.Report($"{software.Name} instalado correctamente");
                    software.Status = "Instalado";
                    software.IsInstalled = true;
                }
                else
                {
                    _logService.LogError($"Error en instalación de {software.Name}: {result.Message}");
                    progress?.Report($"Error instalando {software.Name}");
                    software.Status = "Error";
                }

                return result;
            }
            catch (Exception ex)
            {
                _logService.LogError($"Excepción al instalar {software.Name}", ex);
                software.Status = "Error";
                return OperationResult.Fail($"Error instalando {software.Name}", ex.Message, ex);
            }
            finally
            {
                software.IsInstalling = false;
            }
        }

        private async Task<OperationResult> InstallMsi(SoftwareItem software, bool silent)
        {
            try
            {
                var arguments = silent
                    ? $"/i \"{software.FullPath}\" /qn /norestart"
                    : $"/i \"{software.FullPath}\" /passive";

                var processInfo = new ProcessStartInfo
                {
                    FileName = "msiexec.exe",
                    Arguments = arguments,
                    UseShellExecute = false,
                    CreateNoWindow = silent,
                    RedirectStandardOutput = silent,
                    RedirectStandardError = silent
                };

                using var process = Process.Start(processInfo);
                if (process == null)
                {
                    return OperationResult.Fail("No se pudo iniciar el instalador MSI");
                }

                await process.WaitForExitAsync();

                // Código de salida 0 = éxito, 3010 = éxito con reinicio requerido
                if (process.ExitCode == 0 || process.ExitCode == 3010)
                {
                    return OperationResult.Ok($"{software.Name} instalado exitosamente");
                }

                return OperationResult.Fail(
                    $"Instalación MSI falló con código {process.ExitCode}",
                    silent ? "Intentando instalación en modo interactivo..." : ""
                );
            }
            catch (Exception ex)
            {
                return OperationResult.Fail("Error al instalar MSI", ex.Message, ex);
            }
        }

        private async Task<OperationResult> InstallExe(SoftwareItem software, bool silent)
        {
            try
            {
                // Detectar si es un instalador Ninite (no soporta modo silencioso en versión gratuita)
                // Ninite arroja error 0x80004005 si se usan switches como /s, /S, /silent, etc.
                bool isNinite = software.FileName.ToLower().Contains("ninite");

                if (isNinite)
                {
                    _logService.LogInfo($"Detectado instalador Ninite: {software.Name}, forzando modo interactivo");

                    // Ninite siempre en modo interactivo (sin argumentos)
                    var niniteProcessInfo = new ProcessStartInfo
                    {
                        FileName = software.FullPath,
                        Arguments = "", // Sin argumentos para evitar error 0x80004005
                        UseShellExecute = true,
                        CreateNoWindow = false
                    };

                    using var niniteProcess = Process.Start(niniteProcessInfo);
                    if (niniteProcess == null)
                    {
                        return OperationResult.Fail("No se pudo iniciar el instalador Ninite");
                    }

                    return OperationResult.Ok($"{software.Name} - instalación Ninite iniciada (modo interactivo)");
                }

                // Para instaladores no-Ninite, continuar con lógica normal
                // Detectar instaladores de Office que requieren configuración especial
                bool isOffice = software.FileName.ToLower().Contains("office") ||
                               software.FileName.ToLower().Contains("o365") ||
                               software.FileName.ToLower().Contains("microsoft365");

                string arguments;
                if (isOffice && silent)
                {
                    // Office installers generalmente usan switches específicos
                    // Probar primero con switches comunes de Office
                    _logService.LogInfo($"Detectado instalador de Office: {software.Name}");
                    arguments = "/configure"; // Office Deployment Tool usa /configure
                }
                else
                {
                    var silentArgs = new[] { "/S", "/silent", "/quiet", "/q", "/qn" };
                    arguments = silent ? silentArgs[0] : "";
                }

                var processInfo = new ProcessStartInfo
                {
                    FileName = software.FullPath,
                    Arguments = arguments,
                    UseShellExecute = !silent,
                    CreateNoWindow = silent,
                    RedirectStandardOutput = silent,
                    RedirectStandardError = silent
                };

                using var process = Process.Start(processInfo);
                if (process == null)
                {
                    return OperationResult.Fail("No se pudo iniciar el instalador EXE");
                }

                // Si es instalación interactiva, no esperamos (el usuario interactúa)
                if (!silent)
                {
                    return OperationResult.Ok($"{software.Name} - instalación interactiva iniciada");
                }

                await process.WaitForExitAsync();

                // Códigos de salida exitosos comunes
                // 0 = éxito
                // 3010 = éxito con reinicio requerido
                // 1641 = éxito, instalador inició reinicio
                if (process.ExitCode == 0 || process.ExitCode == 3010 || process.ExitCode == 1641)
                {
                    var message = process.ExitCode == 3010 || process.ExitCode == 1641
                        ? $"{software.Name} instalado (reinicio requerido)"
                        : $"{software.Name} instalado exitosamente";
                    return OperationResult.Ok(message);
                }

                // Si falla con primer argumento silencioso, reportar para intentar interactivo
                _logService.LogWarning($"Instalación silenciosa falló con código {process.ExitCode}, intentando modo interactivo...");
                return OperationResult.Fail(
                    $"Instalación EXE falló con código {process.ExitCode}",
                    "silent mode failed"
                );
            }
            catch (Exception ex)
            {
                return OperationResult.Fail("Error al instalar EXE", ex.Message, ex);
            }
        }

        public async Task<List<OperationResult>> InstallSelectedSoftware(
            List<SoftwareItem> softwareList,
            IProgress<string>? progress = null)
        {
            var results = new List<OperationResult>();
            var selectedSoftware = softwareList.Where(s => s.IsSelected).ToList();

            _logService.LogInfo($"Instalando {selectedSoftware.Count} aplicaciones seleccionadas");

            foreach (var software in selectedSoftware)
            {
                software.IsInstalling = true;
                var result = await InstallSoftware(software, progress);
                results.Add(result);
            }

            return results;
        }
    }
}
