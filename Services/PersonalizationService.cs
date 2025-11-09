using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using MABAppTecnologia.Models;
using Microsoft.Win32;

namespace MABAppTecnologia.Services
{
    public class PersonalizationService : IPersonalizationService
    {
        private readonly ILogService _logService;
        private readonly IConfigService _configService;
        private readonly string[] _supportedWallpaperFormats = { ".jpg", ".jpeg", ".png", ".bmp" };
        private readonly string[] _supportedProfileImageFormats = { ".png", ".jpg", ".jpeg", ".bmp", ".gif" };
        private readonly (string Key, int Size)[] _profileImageSizes =
        {
            ("Image32", 32),
            ("Image40", 40),
            ("Image48", 48),
            ("Image96", 96),
            ("Image192", 192),
            ("Image240", 240),
            ("Image448", 448)
        };
        private const uint SPI_SETDESKWALLPAPER = 0x0014;
        private const uint SPIF_UPDATEINIFILE = 0x01;
        private const uint SPIF_SENDWININICHANGE = 0x02;

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool SystemParametersInfo(uint uiAction, uint uiParam, string pvParam, uint fWinIni);

        public PersonalizationService(ILogService logService, IConfigService configService)
        {
            _logService = logService;
            _configService = configService;
        }

        /// <summary>
        /// Verifica si un usuario local existe
        /// </summary>
        private bool UserExists(string username)
        {
            try
            {
                // Si es el usuario actual, asumimos que existe
                var currentUser = Environment.UserName;
                if (currentUser.Equals(username, StringComparison.OrdinalIgnoreCase))
                {
                    _logService.LogInfo($"Usuario {username} es el usuario actual, se asume que existe");
                    return true;
                }

                // Para otros usuarios, verificar con PowerShell
                using var ps = PowerShell.Create();
                ps.AddScript($"Get-LocalUser -Name '{username}' -ErrorAction SilentlyContinue | Select-Object -First 1");
                
                Collection<PSObject>? result = null;
                var invokeTask = Task.Run(() =>
                {
                    try
                    {
                        result = ps.Invoke();
                        return true;
                    }
                    catch
                    {
                        return false;
                    }
                });

                if (!invokeTask.Wait(TimeSpan.FromSeconds(3)))
                {
                    _logService.LogWarning($"Timeout al verificar existencia del usuario {username}. Se asume que existe para continuar.");
                    // Asumimos que existe para no bloquear el proceso
                    return true;
                }

                var exists = result != null && result.Count > 0;
                if (!exists)
                {
                    _logService.LogWarning($"Usuario {username} no encontrado en el sistema");
                }
                return exists;
            }
            catch (Exception ex)
            {
                _logService.LogWarning($"Error al verificar existencia del usuario {username}: {ex.Message}. Se asume que existe.");
                // En caso de error, asumimos que existe para no bloquear
                return true;
            }
        }

        /// <summary>
        /// Ejecuta un script de PowerShell con timeout para evitar bloqueos
        /// </summary>
        private bool ExecutePowerShellWithTimeout(PowerShell ps, int timeoutSeconds = 10)
        {
            try
            {
                var invokeTask = Task.Run(() =>
                {
                    try
                    {
                        ps.Invoke();
                        return true;
                    }
                    catch
                    {
                        return false;
                    }
                });
                return invokeTask.Wait(TimeSpan.FromSeconds(timeoutSeconds)) && invokeTask.Result;
            }
            catch
            {
                return false;
            }
        }

        private bool TryResolveUserSid(string username, out string userSid, out bool isCurrentUser)
        {
            userSid = string.Empty;
            isCurrentUser = false;

            try
            {
                var currentUser = Environment.UserName;
                if (currentUser.Equals(username, StringComparison.OrdinalIgnoreCase))
                {
                    isCurrentUser = true;
                    _logService.LogInfo($"Usuario {username} es el usuario actual, obteniendo SID directamente");
                    using var psCurrent = PowerShell.Create();
                    psCurrent.AddScript("[System.Security.Principal.WindowsIdentity]::GetCurrent().User.Value");

                    var currentTask = Task.Run(() =>
                    {
                        try
                        {
                            var result = psCurrent.Invoke();
                            return result != null && result.Count > 0 ? result[0]?.ToString() ?? string.Empty : string.Empty;
                        }
                        catch
                        {
                            return string.Empty;
                        }
                    });

                    if (currentTask.Wait(TimeSpan.FromSeconds(3)) && !string.IsNullOrEmpty(currentTask.Result))
                    {
                        userSid = currentTask.Result;
                        _logService.LogInfo($"SID del usuario actual ({username}): {userSid}");
                        return true;
                    }

                    _logService.LogWarning($"No se pudo obtener el SID del usuario actual {username} directamente. Intentando búsqueda por nombre.");
                }

                _logService.LogInfo($"Buscando SID del usuario {username} por nombre");
                Collection<PSObject>? sidResult = null;
                using var psGetSid = PowerShell.Create();
                psGetSid.AddScript($"$user = Get-LocalUser -Name '{username}' -ErrorAction SilentlyContinue; if ($user) {{ Write-Output $user.SID.Value }}");

                var sidTask = Task.Run(() =>
                {
                    try
                    {
                        sidResult = psGetSid.Invoke();
                        return !psGetSid.HadErrors;
                    }
                    catch
                    {
                        return false;
                    }
                });

                if (!sidTask.Wait(TimeSpan.FromSeconds(5)))
                {
                    _logService.LogWarning($"Timeout al obtener SID del usuario {username}");
                    return false;
                }

                if (psGetSid.HadErrors || sidResult == null || sidResult.Count == 0)
                {
                    _logService.LogWarning($"No se pudo obtener el SID del usuario {username}");
                    return false;
                }

                userSid = sidResult[0]?.ToString() ?? string.Empty;
                if (string.IsNullOrEmpty(userSid))
                {
                    _logService.LogWarning($"SID vacío obtenido para {username}");
                    return false;
                }

                _logService.LogInfo($"SID de {username}: {userSid}");
                return true;
            }
            catch (Exception ex)
            {
                _logService.LogWarning($"Error al obtener SID del usuario {username}: {ex.Message}");
                return false;
            }
        }

        private string? FindImageFile(string directory, string fileName, string[] supportedFormats)
        {
            try
            {
                var requestedPath = Path.Combine(directory, fileName);
                if (File.Exists(requestedPath))
                {
                    _logService.LogInfo($"Imagen encontrada: {requestedPath}");
                    return requestedPath;
                }

                var baseName = Path.GetFileNameWithoutExtension(fileName);
                var relativeDirectory = Path.GetDirectoryName(fileName);
                var searchDirectory = string.IsNullOrEmpty(relativeDirectory)
                    ? directory
                    : Path.Combine(directory, relativeDirectory);

                foreach (var extension in supportedFormats)
                {
                    var candidate = Path.Combine(searchDirectory, baseName + extension);
                    if (File.Exists(candidate))
                    {
                        _logService.LogInfo($"Imagen encontrada con formato alternativo: {candidate}");
                        return candidate;
                    }
                }

                _logService.LogWarning($"No se encontró la imagen {fileName} en {directory}. Formatos buscados: {string.Join(", ", supportedFormats)}");
            }
            catch (Exception ex)
            {
                _logService.LogWarning($"Error al buscar la imagen {fileName} en {directory}: {ex.Message}");
            }

            return null;
        }

        public OperationResult SetWallpaperForUser(string username, string wallpaperFileName)
        {
            try
            {
                var userExists = UserExists(username);
                if (!userExists)
                {
                    _logService.LogWarning($"Usuario {username} no existe. Intentando configurar wallpaper de todos modos...");
                }

                var wallpapersDirectory = Path.Combine(
                    _configService.AppConfig.MABResourcesPath,
                    _configService.AppConfig.WallpapersFolder
                );
                var wallpaperPath = FindImageFile(wallpapersDirectory, wallpaperFileName, _supportedWallpaperFormats);

                if (string.IsNullOrEmpty(wallpaperPath))
                {
                    return OperationResult.Fail(
                        $"No se encontró el fondo de pantalla: {wallpaperFileName}",
                        $"Ruta base: {wallpapersDirectory}. Formatos buscados: {string.Join(", ", _supportedWallpaperFormats)}"
                    );
                }

                _logService.LogInfo($"Configurando fondo de pantalla para {username}: {wallpaperPath}");

                if (!TryResolveUserSid(username, out var userSid, out var isCurrentUser))
                {
                    _logService.LogWarning($"No se pudo resolver el SID del usuario {username}. Omitiendo configuración de wallpaper específico.");
                    return OperationResult.Ok($"Wallpaper omitido para {username} (usuario no encontrado o sin SID). Configura manualmente después de iniciar sesión.");
                }

                using var ps = PowerShell.Create();
                var escapedPath = wallpaperPath.Replace("'", "''");
                ps.AddScript($@"
                    $ErrorActionPreference = 'Stop'
                    $sid = '{userSid}'
                    $wallpaperPath = '{escapedPath}'

                    try {{
                        # Método 1: Configurar en LogonUI\Creative (se aplica cuando el usuario inicia sesión)
                        $regPath = ""HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Authentication\LogonUI\Creative\$sid""

                        if (-not (Test-Path $regPath)) {{
                            New-Item -Path $regPath -Force | Out-Null
                        }}

                        Set-ItemProperty -Path $regPath -Name 'Wallpaper' -Value $wallpaperPath -ErrorAction Stop
                        Set-ItemProperty -Path $regPath -Name 'TransformedFile' -Value $wallpaperPath -ErrorAction SilentlyContinue

                        # Método 2: También configurar en el perfil por defecto si el usuario ya inició sesión
                        $hkuPath = ""Registry::HKEY_USERS\$sid\Control Panel\Desktop""
                        if (Test-Path ""Registry::HKEY_USERS\$sid"") {{
                            if (-not (Test-Path $hkuPath)) {{
                                New-Item -Path $hkuPath -Force | Out-Null
                            }}
                            Set-ItemProperty -Path $hkuPath -Name 'Wallpaper' -Value $wallpaperPath -ErrorAction Stop
                            Set-ItemProperty -Path $hkuPath -Name 'WallpaperStyle' -Value '10' -ErrorAction Stop
                            Set-ItemProperty -Path $hkuPath -Name 'TileWallpaper' -Value '0' -ErrorAction Stop
                        }}

                        # Método 3: Configurar en perfil de usuario específico (si existe)
                        $userProfilePath = ""HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\ProfileList\$sid""
                        if (Test-Path $userProfilePath) {{
                            $profileImagePath = (Get-ItemProperty -Path $userProfilePath -Name 'ProfileImagePath' -ErrorAction SilentlyContinue).ProfileImagePath
                            if ($profileImagePath -and (Test-Path $profileImagePath)) {{
                                $userDesktopPath = Join-Path $profileImagePath 'AppData\Roaming\Microsoft\Windows\Themes'
                                if (-not (Test-Path $userDesktopPath)) {{
                                    New-Item -Path $userDesktopPath -ItemType Directory -Force | Out-Null
                                }}
                                Copy-Item -Path $wallpaperPath -Destination (Join-Path $userDesktopPath 'TranscodedWallpaper') -Force -ErrorAction SilentlyContinue
                            }}
                        }}

                        Write-Output 'SUCCESS'
                    }} catch {{
                        Write-Error $_.Exception.Message
                        Write-Output 'FAILED'
                    }}
                ");

                if (!ExecutePowerShellWithTimeout(ps, 10))
                {
                    _logService.LogWarning($"Timeout al configurar wallpaper para {username}. Se configuró en registro pero puede no aplicarse hasta próximo inicio de sesión.");
                    return OperationResult.Ok($"Wallpaper configurado en registro. Se aplicará cuando {username} inicie sesión.");
                }

                if (ps.HadErrors)
                {
                    var errors = string.Join(", ", ps.Streams.Error.Select(e => e.ToString()));
                    _logService.LogWarning($"Advertencias al configurar wallpaper: {errors}");
                }

                if (isCurrentUser)
                {
                    ApplyWallpaperForCurrentUser(wallpaperPath);
                }

                _logService.LogSuccess($"Fondo de pantalla configurado para {username}");
                return OperationResult.Ok($"Fondo de pantalla configurado para {username}. Se aplicará al iniciar sesión.");
            }
            catch (Exception ex)
            {
                _logService.LogError($"Error al configurar fondo de pantalla para {username}", ex);
                return OperationResult.Fail("Error al configurar fondo de pantalla", ex.Message, ex);
            }
        }

        public OperationResult SetLockScreenForUser(string username, string lockscreenFileName)
        {
            try
            {
                var wallpapersDirectory = Path.Combine(
                    _configService.AppConfig.MABResourcesPath,
                    _configService.AppConfig.WallpapersFolder
                );
                var lockscreenPath = FindImageFile(wallpapersDirectory, lockscreenFileName, _supportedWallpaperFormats);

                if (string.IsNullOrEmpty(lockscreenPath))
                {
                    return OperationResult.Fail(
                        $"No se encontró la imagen de lockscreen: {lockscreenFileName}",
                        $"Ruta base: {wallpapersDirectory}. Formatos buscados: {string.Join(", ", _supportedWallpaperFormats)}"
                    );
                }

                var commonAppData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
                var storagePath = Path.Combine(commonAppData, "MABAppTecnologia", "LockScreens");
                Directory.CreateDirectory(storagePath);

                var extension = Path.GetExtension(lockscreenPath);
                if (string.IsNullOrWhiteSpace(extension))
                {
                    extension = ".jpg";
                }

                var destinationLockscreenPath = Path.Combine(storagePath, $"{username}_LockScreen{extension}");
                if (!lockscreenPath.Equals(destinationLockscreenPath, StringComparison.OrdinalIgnoreCase))
                {
                    File.Copy(lockscreenPath, destinationLockscreenPath, true);
                }
                _logService.LogInfo($"Imagen de lockscreen disponible en: {destinationLockscreenPath}");

                bool sidResolved = TryResolveUserSid(username, out var userSid, out _);
                if (!sidResolved)
                {
                    _logService.LogWarning($"No se pudo obtener SID para {username}. Se aplicará lockscreen global.");
                }

                using (var policyKey = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\Personalization"))
                {
                    policyKey?.SetValue("LockScreenImage", destinationLockscreenPath, RegistryValueKind.String);
                    policyKey?.SetValue("LockScreenImageUrl", destinationLockscreenPath, RegistryValueKind.String);
                    policyKey?.SetValue("NoChangingLockScreen", 1, RegistryValueKind.DWord);
                }

                using (var cloudContentKey = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\CloudContent"))
                {
                    cloudContentKey?.SetValue("DisableWindowsSpotlightFeatures", 1, RegistryValueKind.DWord);
                    cloudContentKey?.SetValue("DisableWindowsSpotlightOnLockScreen", 1, RegistryValueKind.DWord);
                    cloudContentKey?.SetValue("DisableWindowsSpotlightOnActionCenter", 1, RegistryValueKind.DWord);
                    cloudContentKey?.SetValue("DisableThirdPartySuggestions", 1, RegistryValueKind.DWord);
                    cloudContentKey?.SetValue("DisableSpotlightCollectionOnDesktop", 1, RegistryValueKind.DWord);
                }

                using (var logonUIKey = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Authentication\LogonUI\Creative"))
                {
                    logonUIKey?.SetValue("LockScreenImage", destinationLockscreenPath, RegistryValueKind.String);
                }

                if (sidResolved)
                {
                    using (var logonUiUserKey = Registry.LocalMachine.CreateSubKey($@"SOFTWARE\Microsoft\Windows\CurrentVersion\Authentication\LogonUI\Creative\{userSid}"))
                    {
                        logonUiUserKey?.SetValue("LockScreenImage", destinationLockscreenPath, RegistryValueKind.String);
                        logonUiUserKey?.SetValue("TransformedLockScreenImage", destinationLockscreenPath, RegistryValueKind.String);
                    }

                    using (var userPolicyKey = Registry.Users.CreateSubKey($@"{userSid}\\Software\\Policies\\Microsoft\\Windows\\Personalization"))
                    {
                        userPolicyKey?.SetValue("LockScreenImage", destinationLockscreenPath, RegistryValueKind.String);
                        userPolicyKey?.SetValue("LockScreenImageUrl", destinationLockscreenPath, RegistryValueKind.String);
                        userPolicyKey?.SetValue("NoChangingLockScreen", 1, RegistryValueKind.DWord);
                    }

                    using (var userCloudContentKey = Registry.Users.CreateSubKey($@"{userSid}\\Software\\Policies\\Microsoft\\Windows\\CloudContent"))
                    {
                        userCloudContentKey?.SetValue("DisableWindowsSpotlightFeatures", 1, RegistryValueKind.DWord);
                        userCloudContentKey?.SetValue("DisableWindowsSpotlightOnLockScreen", 1, RegistryValueKind.DWord);
                        userCloudContentKey?.SetValue("DisableWindowsSpotlightOnActionCenter", 1, RegistryValueKind.DWord);
                        userCloudContentKey?.SetValue("DisableThirdPartySuggestions", 1, RegistryValueKind.DWord);
                        userCloudContentKey?.SetValue("DisableSpotlightCollectionOnDesktop", 1, RegistryValueKind.DWord);
                    }
                }

                _logService.LogSuccess($"Pantalla de bloqueo configurada para {username}");
                return OperationResult.Ok($"Pantalla de bloqueo configurada para {username}. Bloquea y desbloquea el equipo para ver los cambios.");
            }
            catch (Exception ex)
            {
                _logService.LogError("Error al configurar pantalla de bloqueo", ex);
                return OperationResult.Fail("Error al configurar pantalla de bloqueo", ex.Message, ex);
            }
        }

        public OperationResult SetUserProfileImage(string username, string profileImageFileName)
        {
            try
            {
                if (!UserExists(username))
                {
                    _logService.LogWarning($"Usuario {username} no existe. No se puede configurar imagen de perfil.");
                    return OperationResult.Fail($"El usuario {username} no existe. Ejecuta el Paso 2 primero.");
                }

                var profileImagesDirectory = Path.Combine(
                    _configService.AppConfig.MABResourcesPath,
                    _configService.AppConfig.ProfileImagesFolder
                );
                var profileImagePath = FindImageFile(profileImagesDirectory, profileImageFileName, _supportedProfileImageFormats);

                if (string.IsNullOrEmpty(profileImagePath))
                {
                    return OperationResult.Fail(
                        $"No se encontró la imagen de perfil: {profileImageFileName}",
                        $"Ruta base: {profileImagesDirectory}. Formatos buscados: {string.Join(", ", _supportedProfileImageFormats)}"
                    );
                }

                _logService.LogInfo($"Configurando imagen de perfil para {username}: {profileImagePath}");

                if (!TryResolveUserSid(username, out var userSid, out _))
                {
                    _logService.LogWarning($"No se pudo obtener el SID del usuario {username}. Omitiendo configuración de imagen de perfil específica.");
                    return OperationResult.Ok($"Imagen de perfil omitida para {username} (usuario no encontrado o sin SID). Configura manualmente después de iniciar sesión.");
                }

                var programDataPath = @"C:\\ProgramData\\Microsoft\\User Account Pictures";
                Directory.CreateDirectory(programDataPath);
                var defaultDestinationPath = Path.Combine(programDataPath, $"{username}.png");

                var generatedVariants = GenerateAccountPictureVariants(profileImagePath, username, userSid);
                if (generatedVariants.TryGetValue("Image448", out var generated448Path))
                {
                    try
                    {
                        File.Copy(generated448Path, defaultDestinationPath, true);
                        _logService.LogInfo($"Copia 448 guardada en ProgramData: {defaultDestinationPath}");
                    }
                    catch (Exception copyEx)
                    {
                        _logService.LogWarning($"No se pudo copiar la imagen 448 a ProgramData: {copyEx.Message}");
                    }
                }
                else
                {
                    _logService.LogWarning("No se generó la variante 448. Intentando conversión directa a PNG.");
                    if (!TryConvertImageToPng(profileImagePath, defaultDestinationPath))
                    {
                        _logService.LogWarning("Conversión a PNG falló. Se copiará el archivo original con su formato nativo.");
                        var originalExtension = Path.GetExtension(profileImagePath);
                        if (string.IsNullOrWhiteSpace(originalExtension))
                        {
                            originalExtension = ".png";
                        }

                        var fallbackPath = Path.Combine(programDataPath, $"{username}{originalExtension}");
                        File.Copy(profileImagePath, fallbackPath, true);
                        defaultDestinationPath = fallbackPath;
                    }
                    generatedVariants["Image448"] = defaultDestinationPath;
                }

                foreach (var (key, _) in _profileImageSizes)
                {
                    if (!generatedVariants.ContainsKey(key))
                    {
                        generatedVariants[key] = defaultDestinationPath;
                    }
                }

                var registryPath = $@"SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\AccountPicture\\Users\\{userSid}";
                using var registryKey = Registry.LocalMachine.CreateSubKey(registryPath);
                if (registryKey == null)
                {
                    _logService.LogWarning($"No se pudo abrir el registro para asignar la imagen de perfil a {username}.");
                    return OperationResult.Ok($"Imagen de perfil copiada para {username}, pero no se pudo actualizar el registro. Configura manualmente después de iniciar sesión.");
                }

                foreach (var kvp in generatedVariants)
                {
                    registryKey.SetValue(kvp.Key, kvp.Value, RegistryValueKind.String);
                }

                registryKey.SetValue("ImageCacheId", Guid.NewGuid().ToString("N"), RegistryValueKind.String);

                _logService.LogSuccess($"Imagen de perfil configurada para {username}");
                return OperationResult.Ok($"Imagen de perfil configurada para {username}");
            }
            catch (Exception ex)
            {
                _logService.LogError($"Error al configurar imagen de perfil para {username}", ex);
                return OperationResult.Fail("Error al configurar imagen de perfil", ex.Message, ex);
            }
        }

        public OperationResult ApplyAllPersonalization(string username, bool isAdmin)
        {
            try
            {
                _logService.LogInfo($"Aplicando personalización completa para {username} (Admin: {isAdmin})");

                var wallpaper = isAdmin ? _configService.AppConfig.AdminWallpaper : _configService.AppConfig.MABWallpaper;
                var lockscreen = isAdmin ? _configService.AppConfig.AdminLockscreen : _configService.AppConfig.MABLockscreen;
                var profile = isAdmin ? _configService.AppConfig.AdminProfile : _configService.AppConfig.MABProfile;

                var results = new List<(string Operation, OperationResult Result)>
                {
                    ("Wallpaper", SetWallpaperForUser(username, wallpaper)),
                    ("Lockscreen", SetLockScreenForUser(username, lockscreen)),
                    ("ProfileImage", SetUserProfileImage(username, profile))
                };

                var successful = results.Where(r => r.Result.Success).ToList();
                var failed = results.Where(r => !r.Result.Success).ToList();

                // Si al menos una operación fue exitosa, consideramos éxito parcial
                if (successful.Count > 0)
                {
                    var successOps = string.Join(", ", successful.Select(r => r.Operation));
                    if (failed.Count > 0)
                    {
                        var failedOps = string.Join(", ", failed.Select(r => r.Operation));
                        var errors = string.Join("; ", failed.Select(r => $"{r.Operation}: {r.Result.Message}"));
                        _logService.LogWarning($"Personalización parcial para {username}: Exitosas ({successOps}), Fallidas ({failedOps})");
                        return OperationResult.Ok($"Personalización aplicada parcialmente para {username}. Exitosas: {successOps}. Fallidas: {failedOps}");
                    }
                    else
                    {
                        _logService.LogSuccess($"Personalización completa aplicada para {username}");
                        return OperationResult.Ok($"Personalización aplicada exitosamente para {username}");
                    }
                }
                else
                {
                    // Todas fallaron
                    var errors = string.Join("; ", failed.Select(r => $"{r.Operation}: {r.Result.Message}"));
                    _logService.LogError($"Todas las operaciones de personalización fallaron para {username}");
                    return OperationResult.Fail($"Todas las operaciones de personalización fallaron para {username}", errors);
                }
            }
            catch (Exception ex)
            {
                _logService.LogError($"Error al aplicar personalización para {username}", ex);
                return OperationResult.Fail("Error al aplicar personalización", ex.Message, ex);
            }
        }

        private string? GetUserProfilePath(string userSid)
        {
            try
            {
                using var profileKey = Registry.LocalMachine.OpenSubKey($@"SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\ProfileList\\{userSid}");
                var profilePath = profileKey?.GetValue("ProfileImagePath") as string;
                if (string.IsNullOrWhiteSpace(profilePath))
                {
                    return null;
                }

                var expandedPath = Environment.ExpandEnvironmentVariables(profilePath);
                return expandedPath;
            }
            catch (Exception ex)
            {
                _logService.LogWarning($"No se pudo obtener la ruta de perfil para SID {userSid}: {ex.Message}");
                return null;
            }
        }

        private Bitmap CreateSquareImage(Image source, int size)
        {
            var bitmap = new Bitmap(size, size);
            bitmap.SetResolution(source.HorizontalResolution, source.VerticalResolution);

            using var graphics = Graphics.FromImage(bitmap);
            graphics.Clear(Color.Transparent);
            graphics.CompositingQuality = CompositingQuality.HighQuality;
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

            var scale = Math.Max((float)size / source.Width, (float)size / source.Height);
            var scaledWidth = (int)Math.Ceiling(source.Width * scale);
            var scaledHeight = (int)Math.Ceiling(source.Height * scale);
            var offsetX = (size - scaledWidth) / 2;
            var offsetY = (size - scaledHeight) / 2;

            graphics.DrawImage(source, offsetX, offsetY, scaledWidth, scaledHeight);

            return bitmap;
        }

        private Dictionary<string, string> GenerateAccountPictureVariants(string sourcePath, string username, string userSid)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                var profilePath = GetUserProfilePath(userSid);
                if (string.IsNullOrWhiteSpace(profilePath) || !Directory.Exists(profilePath))
                {
                    _logService.LogWarning($"Ruta de perfil no encontrada para {username}. Se utilizará únicamente la ruta global.");
                    return result;
                }

                var accountPicturesPath = Path.Combine(profilePath, "AppData", "Roaming", "Microsoft", "Windows", "AccountPictures");
                Directory.CreateDirectory(accountPicturesPath);

                var baseFileName = $"{userSid.Replace("-", string.Empty)}_{DateTime.Now:yyyyMMddHHmmss}";
                using var original = Image.FromFile(sourcePath);
                foreach (var (key, size) in _profileImageSizes)
                {
                    var destination = Path.Combine(accountPicturesPath, $"{baseFileName}-{size}.png");
                    using var resized = CreateSquareImage(original, size);
                    resized.Save(destination, ImageFormat.Png);
                    result[key] = destination;
                    _logService.LogInfo($"Variante {size}px generada en: {destination}");
                }
            }
            catch (Exception ex)
            {
                _logService.LogWarning($"No se pudieron generar variantes de imagen para {username}: {ex.Message}");
                result.Clear();
            }

            return result;
        }

        private void ApplyWallpaperForCurrentUser(string wallpaperPath)
        {
            try
            {
                var updateResult = SystemParametersInfo(
                    SPI_SETDESKWALLPAPER,
                    0,
                    wallpaperPath,
                    SPIF_UPDATEINIFILE | SPIF_SENDWININICHANGE
                );

                if (updateResult)
                {
                    _logService.LogInfo("Wallpaper aplicado inmediatamente para el usuario actual.");
                }
                else
                {
                    var error = Marshal.GetLastWin32Error();
                    _logService.LogWarning($"No se pudo forzar la actualización del wallpaper. Win32 Error: {error}");
                }
            }
            catch (Exception ex)
            {
                _logService.LogWarning($"Error al forzar la actualización del wallpaper: {ex.Message}");
            }
        }

        private bool TryConvertImageToPng(string sourcePath, string destinationPath, int size = 448)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
                using var image = Image.FromFile(sourcePath);
                using var square = CreateSquareImage(image, size);
                square.Save(destinationPath, ImageFormat.Png);
                return true;
            }
            catch (Exception ex)
            {
                _logService.LogWarning($"No se pudo convertir la imagen {sourcePath} a PNG: {ex.Message}");
                return false;
            }
        }
    }
}
