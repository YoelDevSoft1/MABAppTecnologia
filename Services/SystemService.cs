using System.Management;
using System.Management.Automation;
using System.IO;
using System.Diagnostics;
using Microsoft.Win32;
using MABAppTecnologia.Models;

namespace MABAppTecnologia.Services
{
    public class SystemService
    {
        private readonly LogService _logService;
        private readonly string _optimizerScriptPath;

        public SystemService(LogService logService)
        {
            _logService = logService;
            _optimizerScriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "OptimizerMAB.ps1");
        }

        public string GetComputerSerialNumber()
        {
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT SerialNumber FROM Win32_BIOS");
                foreach (ManagementObject obj in searcher.Get())
                {
                    var serial = obj["SerialNumber"]?.ToString() ?? string.Empty;
                    _logService.LogInfo($"Serial del equipo obtenido: {serial}");
                    return serial.Trim();
                }
            }
            catch (Exception ex)
            {
                _logService.LogError("Error al obtener serial del equipo", ex);
            }
            return string.Empty;
        }

        public string GetComputerManufacturer()
        {
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT Manufacturer FROM Win32_ComputerSystem");
                foreach (ManagementObject obj in searcher.Get())
                {
                    var manufacturer = obj["Manufacturer"]?.ToString() ?? string.Empty;
                    _logService.LogInfo($"Fabricante del equipo: {manufacturer}");
                    return manufacturer.Trim();
                }
            }
            catch (Exception ex)
            {
                _logService.LogError("Error al obtener fabricante del equipo", ex);
            }
            return string.Empty;
        }

        public string GenerateComputerName(string siglas, string serial, string manufacturer, TipoEquipo tipoEquipo = TipoEquipo.Propio)
        {
            // Dell usa los primeros 4 dígitos, otros usan los últimos 4
            var serialPart = string.Empty;

            if (manufacturer.ToUpper().Contains("DELL"))
            {
                serialPart = serial.Length >= 4 ? serial.Substring(0, 4) : serial;
            }
            else
            {
                serialPart = serial.Length >= 4 ? serial.Substring(serial.Length - 4) : serial;
            }

            // Generar nombre según el tipo de equipo
            string computerName;
            switch (tipoEquipo)
            {
                case TipoEquipo.Alquiler:
                    computerName = $"{siglas}-RUB-{serialPart}";
                    break;
                case TipoEquipo.HomeOffice:
                    computerName = $"{siglas}-HOME-{serialPart}";
                    break;
                case TipoEquipo.Propio:
                default:
                    computerName = $"{siglas}-{serialPart}";
                    break;
            }

            _logService.LogInfo($"Nombre de equipo generado ({tipoEquipo}): {computerName}");
            return computerName.ToUpper();
        }

        public OperationResult RenameComputer(string newName)
        {
            try
            {
                _logService.LogInfo($"Intentando renombrar equipo a: {newName}");

                using var ps = PowerShell.Create();
                ps.AddCommand("Rename-Computer")
                  .AddParameter("NewName", newName)
                  .AddParameter("Force");

                var results = ps.Invoke();

                if (ps.HadErrors)
                {
                    var errors = string.Join(", ", ps.Streams.Error.Select(e => e.ToString()));
                    return OperationResult.Fail("Error al renombrar el equipo", errors);
                }

                _logService.LogSuccess($"Equipo renombrado exitosamente a: {newName}");
                return OperationResult.Ok($"Equipo renombrado a {newName}. Se requiere reinicio para aplicar cambios.");
            }
            catch (Exception ex)
            {
                _logService.LogError("Error al renombrar equipo", ex);
                return OperationResult.Fail("Error al renombrar equipo", ex.Message, ex);
            }
        }

        public OperationResult CleanDesktopIcons()
        {
            try
            {
                _logService.LogInfo("Limpiando iconos del escritorio...");

                var desktopPaths = new[]
                {
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory)
                };

                foreach (var desktopPath in desktopPaths)
                {
                    if (Directory.Exists(desktopPath))
                    {
                        var shortcuts = Directory.GetFiles(desktopPath, "*.lnk");
                        foreach (var shortcut in shortcuts)
                        {
                            File.Delete(shortcut);
                        }
                    }
                }

                _logService.LogSuccess("Iconos del escritorio limpiados");
                return OperationResult.Ok("Iconos del escritorio eliminados exitosamente");
            }
            catch (Exception ex)
            {
                _logService.LogError("Error al limpiar iconos del escritorio", ex);
                return OperationResult.Fail("Error al limpiar iconos", ex.Message, ex);
            }
        }

        public OperationResult CleanTaskbar(string? username = null)
        {
            try
            {
                _logService.LogInfo($"Limpiando barra de tareas{(username != null ? $" para usuario {username}" : "")}...");

                using var ps = PowerShell.Create();

                if (string.IsNullOrEmpty(username))
                {
                    // Si no se especifica usuario, limpiar para el usuario actual
                    ps.AddScript(@"
                        # Eliminar todos los shortcuts de la barra de tareas
                        $taskbarPath = ""$env:APPDATA\Microsoft\Internet Explorer\Quick Launch\User Pinned\TaskBar""
                        if (Test-Path $taskbarPath) {
                            Get-ChildItem -Path $taskbarPath -Filter *.lnk | Remove-Item -Force -ErrorAction SilentlyContinue
                        }

                        # Limpiar configuración del registro
                        $key = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\Taskband'
                        Remove-ItemProperty -Path $key -Name 'Favorites' -ErrorAction SilentlyContinue

                        # Deshabilitar búsqueda, vista de tareas, widgets y chat
                        Set-ItemProperty -Path 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Search' -Name 'SearchboxTaskbarMode' -Value 0 -ErrorAction SilentlyContinue
                        Set-ItemProperty -Path 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced' -Name 'ShowTaskViewButton' -Value 0 -ErrorAction SilentlyContinue
                        Set-ItemProperty -Path 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced' -Name 'TaskbarDa' -Value 0 -ErrorAction SilentlyContinue
                        Set-ItemProperty -Path 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced' -Name 'TaskbarMn' -Value 0 -ErrorAction SilentlyContinue

                        # Reiniciar Explorer para aplicar cambios
                        Stop-Process -Name explorer -Force -ErrorAction SilentlyContinue
                    ");
                }
                else
                {
                    // Obtener perfil del usuario
                    ps.AddScript($@"
                        $username = '{username}'

                        # Obtener SID y perfil del usuario
                        $user = New-Object System.Security.Principal.NTAccount($username)
                        try {{
                            $sid = $user.Translate([System.Security.Principal.SecurityIdentifier]).Value

                            # Obtener ruta del perfil
                            $profilePath = (Get-ItemProperty -Path ""HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\ProfileList\$sid"" -Name ProfileImagePath -ErrorAction SilentlyContinue).ProfileImagePath

                            if ($profilePath) {{
                                # Eliminar shortcuts de la barra de tareas del usuario
                                $taskbarPath = Join-Path $profilePath 'AppData\Roaming\Microsoft\Internet Explorer\Quick Launch\User Pinned\TaskBar'
                                if (Test-Path $taskbarPath) {{
                                    Get-ChildItem -Path $taskbarPath -Filter *.lnk | Remove-Item -Force -ErrorAction SilentlyContinue
                                    Write-Host ""Eliminados iconos de taskbar para $username""
                                }}

                                # Limpiar configuración del registro del usuario
                                $taskbandKey = ""Registry::HKEY_USERS\$sid\Software\Microsoft\Windows\CurrentVersion\Explorer\Taskband""
                                if (Test-Path $taskbandKey) {{
                                    Remove-ItemProperty -Path $taskbandKey -Name 'Favorites' -ErrorAction SilentlyContinue
                                }}

                                # Deshabilitar búsqueda en la barra de tareas
                                $searchKey = ""Registry::HKEY_USERS\$sid\Software\Microsoft\Windows\CurrentVersion\Search""
                                if (-not (Test-Path $searchKey)) {{
                                    New-Item -Path $searchKey -Force | Out-Null
                                }}
                                Set-ItemProperty -Path $searchKey -Name 'SearchboxTaskbarMode' -Value 0 -ErrorAction SilentlyContinue

                                # Deshabilitar vista de tareas, widgets y chat
                                $advancedKey = ""Registry::HKEY_USERS\$sid\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced""
                                if (Test-Path $advancedKey) {{
                                    Set-ItemProperty -Path $advancedKey -Name 'ShowTaskViewButton' -Value 0 -ErrorAction SilentlyContinue
                                    Set-ItemProperty -Path $advancedKey -Name 'TaskbarDa' -Value 0 -ErrorAction SilentlyContinue
                                    Set-ItemProperty -Path $advancedKey -Name 'TaskbarMn' -Value 0 -ErrorAction SilentlyContinue
                                }}

                                Write-Output 'SUCCESS'
                            }} else {{
                                Write-Output 'PROFILE_NOT_FOUND'
                            }}
                        }} catch {{
                            Write-Output 'ERROR'
                        }}
                    ");
                }

                ps.Invoke();

                if (ps.HadErrors)
                {
                    var errors = string.Join(", ", ps.Streams.Error.Select(e => e.ToString()));
                    _logService.LogWarning($"Advertencias al limpiar barra de tareas: {errors}");
                }

                _logService.LogSuccess($"Barra de tareas limpiada{(username != null ? $" para {username}" : "")}");

                // Después de limpiar, agregar los iconos predefinidos
                _logService.LogInfo("Agregando iconos predefinidos a la barra de tareas...");
                var pinResult = PinAppsToTaskbar(username);

                if (pinResult.Success)
                {
                    return OperationResult.Ok($"Barra de tareas configurada{(username != null ? $" para {username}" : "")}. {pinResult.Message}");
                }
                else
                {
                    return OperationResult.Ok($"Barra de tareas limpiada{(username != null ? $" para {username}" : "")}. Advertencia al agregar iconos: {pinResult.Message}");
                }
            }
            catch (Exception ex)
            {
                _logService.LogError("Error al limpiar barra de tareas", ex);
                return OperationResult.Fail("Error al limpiar barra de tareas", ex.Message, ex);
            }
        }

        public OperationResult PinAppsToTaskbar(string? username = null)
        {
            try
            {
                _logService.LogInfo($"Agregando aplicaciones a la barra de tareas{(username != null ? $" para usuario {username}" : "")}...");

                var currentUser = Environment.UserName;
                var isCurrentUser = string.IsNullOrEmpty(username) || username.Equals(currentUser, StringComparison.OrdinalIgnoreCase);

                using var ps = PowerShell.Create();

                string script;

                if (isCurrentUser)
                {
                    // Para el usuario actual, usar el método COM tradicional
                    script = @"
                        # Función para pinear aplicación usando COM
                        function Pin-ToTaskbar {
                            param([string]$appPath)

                            if (-not (Test-Path $appPath)) {
                                Write-Output ""DEBUG:No encontrado: $appPath""
                                return $false
                            }

                            Write-Output ""DEBUG:Intentando pinear: $appPath""

                            try {
                                $shell = New-Object -ComObject Shell.Application
                                $folder = $shell.Namespace((Split-Path $appPath))
                                $item = $folder.ParseName((Split-Path $appPath -Leaf))

                                if ($null -eq $item) {
                                    Write-Output ""DEBUG:No se pudo obtener el item para: $appPath""
                                    return $false
                                }

                                $verb = $item.Verbs() | Where-Object { $_.Name -match 'Pin to taskbar|Anclar a la barra de tareas' }
                                if ($verb) {
                                    $verb.DoIt()
                                    Write-Output ""DEBUG:Pineado exitosamente: $appPath""
                                    return $true
                                } else {
                                    Write-Output ""DEBUG:No se encontró verbo de pinear para: $appPath""
                                }
                            } catch {
                                Write-Output ""DEBUG:Error al pinear: $appPath - $($_.Exception.Message)""
                            }
                            return $false
                        }

                        # Buscar aplicaciones
                        $appsToPin = @()

                        # Google Chrome
                        $chrome = @(
                            'C:\Program Files\Google\Chrome\Application\chrome.exe',
                            'C:\Program Files (x86)\Google\Chrome\Application\chrome.exe'
                        ) | Where-Object { Test-Path $_ } | Select-Object -First 1
                        if ($chrome) {
                            $appsToPin += $chrome
                            Write-Output ""DEBUG:Chrome encontrado en: $chrome""
                        } else {
                            Write-Output ""DEBUG:Chrome NO encontrado""
                        }

                        # Explorador de archivos
                        $appsToPin += 'C:\Windows\explorer.exe'
                        Write-Output ""DEBUG:Explorador agregado""

                        # PDFGear
                        $pdfgear = Get-ChildItem 'C:\Program Files\PDFgear' -Recurse -Filter 'PDFgear.exe' -ErrorAction SilentlyContinue | Select-Object -First 1
                        if ($pdfgear) { $appsToPin += $pdfgear.FullName }

                        # PDF24
                        $pdf24 = @(
                            'C:\Program Files\PDF24\pdf24-Editor.exe',
                            'C:\Program Files (x86)\PDF24\pdf24-Editor.exe'
                        ) | Where-Object { Test-Path $_ } | Select-Object -First 1
                        if ($pdf24) { $appsToPin += $pdf24 }

                        # Google Drive
                        $drive = Get-ChildItem ""$env:ProgramFiles\Google\Drive File Stream"" -Recurse -Filter 'GoogleDriveFS.exe' -ErrorAction SilentlyContinue | Select-Object -First 1
                        if ($drive) { $appsToPin += $drive.FullName }

                        # Microsoft Office - Excel
                        $excel = Get-ChildItem 'C:\Program Files\Microsoft Office' -Recurse -Filter 'EXCEL.EXE' -ErrorAction SilentlyContinue | Select-Object -First 1
                        if (-not $excel) {
                            $excel = Get-ChildItem 'C:\Program Files (x86)\Microsoft Office' -Recurse -Filter 'EXCEL.EXE' -ErrorAction SilentlyContinue | Select-Object -First 1
                        }
                        if ($excel) { $appsToPin += $excel.FullName }

                        # Microsoft Office - Word
                        $word = Get-ChildItem 'C:\Program Files\Microsoft Office' -Recurse -Filter 'WINWORD.EXE' -ErrorAction SilentlyContinue | Select-Object -First 1
                        if (-not $word) {
                            $word = Get-ChildItem 'C:\Program Files (x86)\Microsoft Office' -Recurse -Filter 'WINWORD.EXE' -ErrorAction SilentlyContinue | Select-Object -First 1
                        }
                        if ($word) { $appsToPin += $word.FullName }

                        # Microsoft Office - PowerPoint
                        $powerpoint = Get-ChildItem 'C:\Program Files\Microsoft Office' -Recurse -Filter 'POWERPNT.EXE' -ErrorAction SilentlyContinue | Select-Object -First 1
                        if (-not $powerpoint) {
                            $powerpoint = Get-ChildItem 'C:\Program Files (x86)\Microsoft Office' -Recurse -Filter 'POWERPNT.EXE' -ErrorAction SilentlyContinue | Select-Object -First 1
                        }
                        if ($powerpoint) { $appsToPin += $powerpoint.FullName }

                        # Pinear todas las aplicaciones encontradas
                        Write-Output ""DEBUG:Total de aplicaciones encontradas: $($appsToPin.Count)""
                        $pinnedCount = 0
                        foreach ($app in $appsToPin) {
                            if (Pin-ToTaskbar -appPath $app) {
                                $pinnedCount++
                                Start-Sleep -Milliseconds 500
                            }
                        }

                        Write-Output ""PINNED:$pinnedCount""
                    ";
                }
                else
                {
                    // Para otro usuario, crear una tarea programada que se ejecute al iniciar sesión
                    // Esto funciona en Windows 11 porque el pinning se ejecuta en el contexto del usuario
                    script = $@"
                        $targetUsername = '{username}'

                        try {{
                            # Crear script temporal para pinear aplicaciones
                            $scriptContent = @'
# Función para pinear aplicación
function Pin-ToTaskbar {{
    param([string]$appPath)

    if (-not (Test-Path $appPath)) {{
        return $false
    }}

    try {{
        $shell = New-Object -ComObject Shell.Application
        $folder = $shell.Namespace((Split-Path $appPath))
        $item = $folder.ParseName((Split-Path $appPath -Leaf))

        $verb = $item.Verbs() | Where-Object {{ $_.Name -match 'Pin to taskbar|Anclar a la barra de tareas' }}
        if ($verb) {{
            $verb.DoIt()
            Start-Sleep -Milliseconds 500
            return $true
        }}
    }} catch {{
        return $false
    }}
    return $false
}}

# Buscar y pinear aplicaciones
$appsToPin = @()

# Google Chrome
$chrome = @(
    'C:\Program Files\Google\Chrome\Application\chrome.exe',
    'C:\Program Files (x86)\Google\Chrome\Application\chrome.exe'
) | Where-Object {{ Test-Path $_ }} | Select-Object -First 1
if ($chrome) {{ $appsToPin += $chrome }}

# Explorador de archivos
$appsToPin += 'C:\Windows\explorer.exe'

# PDFGear
$pdfgear = Get-ChildItem 'C:\Program Files\PDFgear' -Recurse -Filter 'PDFgear.exe' -ErrorAction SilentlyContinue | Select-Object -First 1
if ($pdfgear) {{ $appsToPin += $pdfgear.FullName }}

# PDF24
$pdf24 = @(
    'C:\Program Files\PDF24\pdf24-Editor.exe',
    'C:\Program Files (x86)\PDF24\pdf24-Editor.exe'
) | Where-Object {{ Test-Path $_ }} | Select-Object -First 1
if ($pdf24) {{ $appsToPin += $pdf24 }}

# Google Drive
$drive = Get-ChildItem ""$env:ProgramFiles\Google\Drive File Stream"" -Recurse -Filter 'GoogleDriveFS.exe' -ErrorAction SilentlyContinue | Select-Object -First 1
if ($drive) {{ $appsToPin += $drive.FullName }}

# Microsoft Office - Excel
$excel = Get-ChildItem 'C:\Program Files\Microsoft Office' -Recurse -Filter 'EXCEL.EXE' -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $excel) {{
    $excel = Get-ChildItem 'C:\Program Files (x86)\Microsoft Office' -Recurse -Filter 'EXCEL.EXE' -ErrorAction SilentlyContinue | Select-Object -First 1
}}
if ($excel) {{ $appsToPin += $excel.FullName }}

# Microsoft Office - Word
$word = Get-ChildItem 'C:\Program Files\Microsoft Office' -Recurse -Filter 'WINWORD.EXE' -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $word) {{
    $word = Get-ChildItem 'C:\Program Files (x86)\Microsoft Office' -Recurse -Filter 'WINWORD.EXE' -ErrorAction SilentlyContinue | Select-Object -First 1
}}
if ($word) {{ $appsToPin += $word.FullName }}

# Microsoft Office - PowerPoint
$powerpoint = Get-ChildItem 'C:\Program Files\Microsoft Office' -Recurse -Filter 'POWERPNT.EXE' -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $powerpoint) {{
    $powerpoint = Get-ChildItem 'C:\Program Files (x86)\Microsoft Office' -Recurse -Filter 'POWERPNT.EXE' -ErrorAction SilentlyContinue | Select-Object -First 1
}}
if ($powerpoint) {{ $appsToPin += $powerpoint.FullName }}

# Pinear aplicaciones
$pinnedCount = 0
foreach ($app in $appsToPin) {{
    if (Pin-ToTaskbar -appPath $app) {{
        $pinnedCount++
    }}
}}

# Eliminar esta tarea programada después de ejecutarse
Unregister-ScheduledTask -TaskName 'MAB_PinTaskbarApps' -Confirm:$false -ErrorAction SilentlyContinue

# Eliminar el script temporal
Remove-Item $PSCommandPath -Force -ErrorAction SilentlyContinue
'@

                            # Guardar script en ubicación temporal
                            $scriptPath = ""C:\Windows\Temp\MAB_PinTaskbar_$targetUsername.ps1""
                            $scriptContent | Out-File -FilePath $scriptPath -Encoding UTF8 -Force

                            # Crear tarea programada usando schtasks.exe (más compatible)
                            $taskName = 'MAB_PinTaskbarApps'

                            # Eliminar tarea existente si la hay
                            schtasks /Delete /TN $taskName /F 2>&1 | Out-Null

                            # Crear nueva tarea
                            $psCommand = 'PowerShell.exe -ExecutionPolicy Bypass -WindowStyle Hidden -File ""' + $scriptPath + '""'

                            schtasks /Create /TN $taskName /TR $psCommand /SC ONLOGON /RU $targetUsername /RL HIGHEST /F | Out-Null

                            if ($LASTEXITCODE -eq 0) {{
                                Write-Output ""SCHEDULED:Task created for $targetUsername. Apps will be pinned at next login.""
                            }} else {{
                                Write-Output ""ERROR:Failed to create scheduled task. Exit code: $LASTEXITCODE""
                            }}

                        }} catch {{
                            Write-Output ""ERROR:$($_.Exception.Message)""
                        }}
                    ";
                }

                ps.AddScript(script);
                var results = ps.Invoke();

                var pinnedCount = 0;
                var hasError = false;
                var errorMsg = "";
                var isScheduled = false;
                var scheduledMsg = "";

                foreach (var result in results)
                {
                    var output = result?.ToString() ?? "";

                    if (output.StartsWith("DEBUG:"))
                    {
                        _logService.LogInfo($"[DEBUG] {output.Replace("DEBUG:", "")}");
                    }
                    else if (output.StartsWith("PINNED:"))
                    {
                        int.TryParse(output.Replace("PINNED:", ""), out pinnedCount);
                        _logService.LogInfo($"Pin result: {output}");
                    }
                    else if (output.StartsWith("SCHEDULED:"))
                    {
                        isScheduled = true;
                        scheduledMsg = output.Replace("SCHEDULED:", "");
                        _logService.LogInfo($"Scheduled result: {output}");
                    }
                    else if (output.StartsWith("ERROR:"))
                    {
                        hasError = true;
                        errorMsg = output.Replace("ERROR:", "");
                        _logService.LogError($"Error result: {output}");
                    }
                    else if (!string.IsNullOrWhiteSpace(output))
                    {
                        _logService.LogInfo($"Pin result: {output}");
                    }
                }

                if (ps.HadErrors)
                {
                    var errors = string.Join(", ", ps.Streams.Error.Select(e => e.ToString()));
                    _logService.LogWarning($"Advertencias al pinear aplicaciones: {errors}");
                }

                if (hasError)
                {
                    _logService.LogError($"Error al pinear aplicaciones: {errorMsg}");
                    return OperationResult.Fail("Error al pinear aplicaciones", errorMsg);
                }

                if (isScheduled)
                {
                    _logService.LogSuccess($"Tarea programada creada: {scheduledMsg}");
                    return OperationResult.Ok($"Tarea programada creada. Las aplicaciones se agregarán a la barra de tareas cuando {username} inicie sesión.");
                }

                _logService.LogSuccess($"Aplicaciones agregadas a la barra de tareas: {pinnedCount}");
                return OperationResult.Ok($"Se agregaron {pinnedCount} aplicaciones a la barra de tareas.");
            }
            catch (Exception ex)
            {
                _logService.LogError("Error al pinear aplicaciones", ex);
                return OperationResult.Fail("Error al pinear aplicaciones", ex.Message, ex);
            }
        }

        #region Optimizaciones Integradas desde OptimizerMAB.ps1

        /// <summary>
        /// Ejecuta el script completo de optimización avanzada OptimizerMAB.ps1
        /// </summary>
        public async Task<OperationResult> RunAdvancedOptimizerAsync(Action<string>? progressCallback = null)
        {
            try
            {
                _logService.LogInfo("Ejecutando optimizador avanzado MAB...");
                progressCallback?.Invoke("Iniciando optimizador avanzado...");

                if (!File.Exists(_optimizerScriptPath))
                {
                    return OperationResult.Fail(
                        "Script no encontrado",
                        $"No se encontró el script OptimizerMAB.ps1 en: {_optimizerScriptPath}"
                    );
                }

                var startInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-ExecutionPolicy Bypass -File \"{_optimizerScriptPath}\"",
                    Verb = "runas", // Ejecutar como administrador
                    UseShellExecute = true,
                    CreateNoWindow = false
                };

                var process = Process.Start(startInfo);
                if (process != null)
                {
                    await Task.Run(() => process.WaitForExit());

                    if (process.ExitCode == 0)
                    {
                        _logService.LogSuccess("Optimizador avanzado ejecutado exitosamente");
                        return OperationResult.Ok("Optimización avanzada completada. Revisa la ventana del script para más detalles.");
                    }
                    else
                    {
                        _logService.LogWarning($"El optimizador finalizó con código: {process.ExitCode}");
                        return OperationResult.Ok($"Optimización completada con código: {process.ExitCode}");
                    }
                }

                return OperationResult.Fail("Error", "No se pudo iniciar el proceso del optimizador");
            }
            catch (Exception ex)
            {
                _logService.LogError("Error al ejecutar optimizador avanzado", ex);
                return OperationResult.Fail("Error al ejecutar optimizador", ex.Message, ex);
            }
        }

        /// <summary>
        /// Aplica optimizaciones de privacidad y telemetría
        /// </summary>
        public OperationResult ApplyPrivacyOptimizations()
        {
            try
            {
                _logService.LogInfo("Aplicando optimizaciones de privacidad...");

                // Deshabilitar telemetría
                SetRegistryValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\DataCollection",
                    "AllowTelemetry", 0, RegistryValueKind.DWord);

                // Deshabilitar sugerencias del sistema
                SetRegistryValue(@"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\ContentDeliveryManager",
                    "SystemPaneSuggestionsEnabled", 0, RegistryValueKind.DWord);

                // Deshabilitar características del consumidor
                SetRegistryValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\CloudContent",
                    "DisableConsumerFeatures", 1, RegistryValueKind.DWord);

                // Deshabilitar Copilot
                SetRegistryValue(@"HKEY_CURRENT_USER\Software\Policies\Microsoft\Windows\WindowsCopilot",
                    "TurnOffWindowsCopilot", 1, RegistryValueKind.DWord);

                // Deshabilitar búsqueda dinámica
                SetRegistryValue(@"HKEY_CURRENT_USER\Software\Policies\Microsoft\Windows\Windows Search",
                    "EnableDynamicContentInWSB", 0, RegistryValueKind.DWord);

                _logService.LogSuccess("Optimizaciones de privacidad aplicadas");
                return OperationResult.Ok("Optimizaciones de privacidad aplicadas correctamente");
            }
            catch (Exception ex)
            {
                _logService.LogError("Error al aplicar optimizaciones de privacidad", ex);
                return OperationResult.Fail("Error en optimizaciones de privacidad", ex.Message, ex);
            }
        }

        /// <summary>
        /// Aplica optimizaciones de rendimiento
        /// </summary>
        public OperationResult ApplyPerformanceOptimizations()
        {
            try
            {
                _logService.LogInfo("Aplicando optimizaciones de rendimiento...");

                // Deshabilitar apps en segundo plano
                SetRegistryValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\AppPrivacy",
                    "LetAppsRunInBackground", 2, RegistryValueKind.DWord);

                SetRegistryValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\BackgroundAccessApplications",
                    "GlobalUserDisabled", 1, RegistryValueKind.DWord);

                // Deshabilitar transparencia
                SetRegistryValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
                    "EnableTransparency", 0, RegistryValueKind.DWord);

                // Deshabilitar animaciones
                SetRegistryValue(@"HKEY_CURRENT_USER\Control Panel\Desktop\WindowMetrics",
                    "MinAnimate", "0", RegistryValueKind.String);

                // Ajustar efectos visuales para mejor rendimiento
                SetRegistryValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects",
                    "VisualFXSetting", 2, RegistryValueKind.DWord);

                // Deshabilitar GameDVR
                SetRegistryValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\GameDVR",
                    "AllowGameDVR", 0, RegistryValueKind.DWord);

                // Configurar plan de energía a Alto Rendimiento
                using var ps = PowerShell.Create();
                ps.AddScript(@"
                    $hp = (powercfg -list) | Select-String -Pattern '(?i)high\s*performance|alto\s*rendimiento'
                    if($hp){
                        $guid = (($hp.Line | Select-String -Pattern '[0-9a-fA-F]{8}-([0-9a-fA-F]{4}-){3}[0-9a-fA-F]{12}').Matches.Value | Select-Object -First 1)
                        if($guid){ powercfg -setactive $guid }
                    }
                ");
                ps.Invoke();

                _logService.LogSuccess("Optimizaciones de rendimiento aplicadas");
                return OperationResult.Ok("Optimizaciones de rendimiento aplicadas correctamente");
            }
            catch (Exception ex)
            {
                _logService.LogError("Error al aplicar optimizaciones de rendimiento", ex);
                return OperationResult.Fail("Error en optimizaciones de rendimiento", ex.Message, ex);
            }
        }

        /// <summary>
        /// Deshabilita servicios de telemetría
        /// </summary>
        public OperationResult DisableTelemetryServices()
        {
            try
            {
                _logService.LogInfo("Deshabilitando servicios de telemetría...");

                var services = new[] { "DiagTrack", "dmwappushservice", "RetailDemo" };

                using var ps = PowerShell.Create();
                foreach (var serviceName in services)
                {
                    try
                    {
                        ps.Commands.Clear();
                        ps.AddCommand("Set-Service")
                          .AddParameter("Name", serviceName)
                          .AddParameter("StartupType", "Disabled")
                          .AddParameter("ErrorAction", "SilentlyContinue");
                        ps.Invoke();

                        ps.Commands.Clear();
                        ps.AddCommand("Stop-Service")
                          .AddParameter("Name", serviceName)
                          .AddParameter("Force")
                          .AddParameter("ErrorAction", "SilentlyContinue");
                        ps.Invoke();

                        _logService.LogInfo($"Servicio {serviceName} deshabilitado");
                    }
                    catch
                    {
                        _logService.LogWarning($"No se pudo deshabilitar servicio {serviceName}");
                    }
                }

                _logService.LogSuccess("Servicios de telemetría deshabilitados");
                return OperationResult.Ok("Servicios de telemetría deshabilitados correctamente");
            }
            catch (Exception ex)
            {
                _logService.LogError("Error al deshabilitar servicios", ex);
                return OperationResult.Fail("Error al deshabilitar servicios", ex.Message, ex);
            }
        }

        /// <summary>
        /// Aplica optimizaciones de UX (deshabilita widgets, chat, etc.)
        /// </summary>
        public OperationResult ApplyUXOptimizations()
        {
            try
            {
                _logService.LogInfo("Aplicando optimizaciones de UX...");

                // Deshabilitar Widgets
                SetRegistryValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Dsh",
                    "AllowWidgets", 0, RegistryValueKind.DWord);

                // Deshabilitar Chat
                SetRegistryValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\Windows Chat",
                    "ChatIcon", 3, RegistryValueKind.DWord);

                // Deshabilitar recomendaciones en el menú Inicio
                SetRegistryValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced",
                    "Start_IrisRecommendations", 0, RegistryValueKind.DWord);

                // Deshabilitar Edge prelaunch
                SetRegistryValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\MicrosoftEdge\Main",
                    "AllowPrelaunch", 0, RegistryValueKind.DWord);

                SetRegistryValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\MicrosoftEdge\TabPreloader",
                    "AllowTabPreloading", 0, RegistryValueKind.DWord);

                _logService.LogSuccess("Optimizaciones de UX aplicadas");
                return OperationResult.Ok("Optimizaciones de UX aplicadas correctamente");
            }
            catch (Exception ex)
            {
                _logService.LogError("Error al aplicar optimizaciones de UX", ex);
                return OperationResult.Fail("Error en optimizaciones de UX", ex.Message, ex);
            }
        }

        /// <summary>
        /// Elimina aplicaciones bloatware de Windows
        /// </summary>
        public async Task<OperationResult> RemoveBloatwareAppsAsync(Action<string>? progressCallback = null)
        {
            try
            {
                _logService.LogInfo("Eliminando bloatware...");
                progressCallback?.Invoke("Eliminando aplicaciones innecesarias...");

                var bloatwareApps = new[]
                {
                    "Microsoft.BingNews",
                    "Microsoft.BingWeather",
                    "Microsoft.GamingApp",
                    "Microsoft.GetHelp",
                    "Microsoft.Getstarted",
                    "Microsoft.Microsoft3DViewer",
                    "Microsoft.MicrosoftSolitaireCollection",
                    "Microsoft.People",
                    "Microsoft.SkypeApp",
                    "Microsoft.Todos",
                    "Microsoft.WindowsMaps",
                    "Microsoft.XboxApp",
                    "Microsoft.XboxGamingOverlay",
                    "Microsoft.XboxGameOverlay",
                    "Microsoft.Xbox.TCUI",
                    "Microsoft.ZuneMusic",
                    "Microsoft.ZuneVideo",
                    "Clipchamp.Clipchamp",
                    "Microsoft.549981C3F5F10"
                };

                using var ps = PowerShell.Create();
                int removed = 0;

                foreach (var app in bloatwareApps)
                {
                    try
                    {
                        progressCallback?.Invoke($"Eliminando {app}...");

                        ps.Commands.Clear();
                        ps.AddScript($@"
                            Get-AppxPackage -AllUsers -Name '{app}' -ErrorAction SilentlyContinue |
                            Remove-AppxPackage -AllUsers -ErrorAction SilentlyContinue
                        ");

                        await Task.Run(() => ps.Invoke());

                        if (!ps.HadErrors)
                        {
                            _logService.LogInfo($"Eliminado: {app}");
                            removed++;
                        }
                    }
                    catch
                    {
                        _logService.LogWarning($"No se pudo eliminar: {app}");
                    }
                }

                _logService.LogSuccess($"Bloatware eliminado: {removed} aplicaciones");
                return OperationResult.Ok($"Se eliminaron {removed} aplicaciones innecesarias");
            }
            catch (Exception ex)
            {
                _logService.LogError("Error al eliminar bloatware", ex);
                return OperationResult.Fail("Error al eliminar bloatware", ex.Message, ex);
            }
        }

        /// <summary>
        /// Optimiza elementos de inicio de Windows
        /// </summary>
        public OperationResult OptimizeStartup()
        {
            try
            {
                _logService.LogInfo("Optimizando inicio de Windows...");

                var whitelist = new[] { "GoogleDriveFS", "GoogleDrive", "OneDrive", "Teams", "MSTeams", "Outlook", "OfficeClickToRun" };

                using var ps = PowerShell.Create();
                ps.AddScript($@"
                    $whitelist = @('{string.Join("','", whitelist)}')
                    $roots = @(
                        'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run',
                        'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Run'
                    )

                    foreach ($root in $roots) {{
                        if (Test-Path $root) {{
                            $disabled = ""$root DisabledByMAB""
                            New-Item -Path $disabled -Force | Out-Null

                            $props = Get-ItemProperty -Path $root -ErrorAction SilentlyContinue
                            if ($props) {{
                                $props.PSObject.Properties | Where-Object {{
                                    $_.Name -notin @('PSPath','PSParentPath','PSChildName','PSDrive','PSProvider')
                                }} | ForEach-Object {{
                                    if ($whitelist -notcontains $_.Name) {{
                                        $val = Get-ItemPropertyValue -Path $root -Name $_.Name -ErrorAction SilentlyContinue
                                        if ($val) {{
                                            New-ItemProperty -Path $disabled -Name $_.Name -Value $val -PropertyType String -Force | Out-Null
                                            Remove-ItemProperty -Path $root -Name $_.Name -Force -ErrorAction SilentlyContinue
                                        }}
                                    }}
                                }}
                            }}
                        }}
                    }}
                ");

                ps.Invoke();

                _logService.LogSuccess("Inicio de Windows optimizado");
                return OperationResult.Ok("Elementos de inicio optimizados (se mantuvieron aplicaciones esenciales)");
            }
            catch (Exception ex)
            {
                _logService.LogError("Error al optimizar inicio", ex);
                return OperationResult.Fail("Error al optimizar inicio", ex.Message, ex);
            }
        }

        /// <summary>
        /// Limpia archivos temporales del sistema
        /// </summary>
        public async Task<OperationResult> CleanTemporaryFilesAsync(Action<string>? progressCallback = null)
        {
            try
            {
                _logService.LogInfo("Limpiando archivos temporales...");
                progressCallback?.Invoke("Limpiando archivos temporales...");

                long totalFreed = 0;

                // Limpiar temp de usuario
                var userTemp = Path.GetTempPath();
                totalFreed += await CleanDirectoryAsync(userTemp, progressCallback);

                // Limpiar temp de Windows
                var windowsTemp = @"C:\Windows\Temp";
                if (Directory.Exists(windowsTemp))
                {
                    totalFreed += await CleanDirectoryAsync(windowsTemp, progressCallback);
                }

                // Ejecutar Disk Cleanup
                using var ps = PowerShell.Create();
                ps.AddScript(@"
                    # Limpiar archivos temporales de Windows Update
                    Stop-Service -Name wuauserv -Force -ErrorAction SilentlyContinue
                    Remove-Item -Path C:\Windows\SoftwareDistribution\Download\* -Recurse -Force -ErrorAction SilentlyContinue
                    Start-Service -Name wuauserv -ErrorAction SilentlyContinue

                    # Vaciar papelera de reciclaje
                    Clear-RecycleBin -Force -ErrorAction SilentlyContinue
                ");

                await Task.Run(() => ps.Invoke());

                var freedMB = totalFreed / (1024 * 1024);
                _logService.LogSuccess($"Archivos temporales eliminados: ~{freedMB} MB liberados");
                return OperationResult.Ok($"Limpieza completada. Aproximadamente {freedMB} MB liberados");
            }
            catch (Exception ex)
            {
                _logService.LogError("Error al limpiar archivos temporales", ex);
                return OperationResult.Fail("Error al limpiar archivos", ex.Message, ex);
            }
        }

        /// <summary>
        /// Aplica todas las optimizaciones básicas
        /// </summary>
        public async Task<OperationResult> ApplyAllBasicOptimizationsAsync(Action<string>? progressCallback = null)
        {
            try
            {
                _logService.LogInfo("=== Iniciando optimización completa del sistema ===");
                progressCallback?.Invoke("Iniciando optimización completa...");

                var results = new List<string>();

                // 1. Privacidad
                progressCallback?.Invoke("Aplicando optimizaciones de privacidad...");
                var result1 = ApplyPrivacyOptimizations();
                if (result1.Success) results.Add("✓ Privacidad");

                // 2. Rendimiento
                progressCallback?.Invoke("Aplicando optimizaciones de rendimiento...");
                var result2 = ApplyPerformanceOptimizations();
                if (result2.Success) results.Add("✓ Rendimiento");

                // 3. Servicios de telemetría
                progressCallback?.Invoke("Deshabilitando servicios de telemetría...");
                var result3 = DisableTelemetryServices();
                if (result3.Success) results.Add("✓ Telemetría");

                // 4. UX
                progressCallback?.Invoke("Aplicando optimizaciones de UX...");
                var result4 = ApplyUXOptimizations();
                if (result4.Success) results.Add("✓ UX");

                // 5. Bloatware
                progressCallback?.Invoke("Eliminando bloatware...");
                var result5 = await RemoveBloatwareAppsAsync(progressCallback);
                if (result5.Success) results.Add("✓ Bloatware");

                // 6. Inicio
                progressCallback?.Invoke("Optimizando inicio...");
                var result6 = OptimizeStartup();
                if (result6.Success) results.Add("✓ Inicio");

                // 7. Archivos temporales
                progressCallback?.Invoke("Limpiando archivos temporales...");
                var result7 = await CleanTemporaryFilesAsync(progressCallback);
                if (result7.Success) results.Add("✓ Archivos temp");

                // 8. Escritorio y barra de tareas
                progressCallback?.Invoke("Limpiando escritorio y barra de tareas...");
                CleanDesktopIcons();
                CleanTaskbar();
                results.Add("✓ Escritorio y barra");

                _logService.LogSuccess($"=== Optimización completa finalizada: {results.Count} módulos aplicados ===");
                progressCallback?.Invoke("Optimización completada");

                return OperationResult.Ok(
                    $"Optimización completa finalizada exitosamente:\n\n{string.Join("\n", results)}\n\nSe recomienda reiniciar el equipo."
                );
            }
            catch (Exception ex)
            {
                _logService.LogError("Error durante la optimización completa", ex);
                return OperationResult.Fail("Error en optimización completa", ex.Message, ex);
            }
        }

        // Versiones síncronas para compatibilidad
        public OperationResult RunAdvancedOptimizer()
        {
            return RunAdvancedOptimizerAsync().GetAwaiter().GetResult();
        }

        public OperationResult RemoveBloatwareApps()
        {
            return RemoveBloatwareAppsAsync().GetAwaiter().GetResult();
        }

        public OperationResult CleanTemporaryFiles()
        {
            return CleanTemporaryFilesAsync().GetAwaiter().GetResult();
        }

        #endregion

        #region Métodos auxiliares

        private void SetRegistryValue(string keyPath, string valueName, object value, RegistryValueKind kind)
        {
            try
            {
                // Separar la raíz de la ruta
                var parts = keyPath.Split('\\', 2);
                if (parts.Length != 2) return;

                RegistryKey? rootKey = parts[0] switch
                {
                    "HKEY_LOCAL_MACHINE" => Registry.LocalMachine,
                    "HKEY_CURRENT_USER" => Registry.CurrentUser,
                    "HKEY_CLASSES_ROOT" => Registry.ClassesRoot,
                    "HKEY_USERS" => Registry.Users,
                    "HKEY_CURRENT_CONFIG" => Registry.CurrentConfig,
                    _ => null
                };

                if (rootKey == null) return;

                using var key = rootKey.CreateSubKey(parts[1], true);
                if (key != null)
                {
                    key.SetValue(valueName, value, kind);
                    _logService.LogInfo($"Registry: {keyPath}\\{valueName} = {value}");
                }
            }
            catch (Exception ex)
            {
                _logService.LogWarning($"No se pudo establecer {keyPath}\\{valueName}: {ex.Message}");
            }
        }

        private async Task<long> CleanDirectoryAsync(string path, Action<string>? progressCallback)
        {
            long totalSize = 0;

            try
            {
                if (!Directory.Exists(path)) return 0;

                var files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories);

                foreach (var file in files)
                {
                    try
                    {
                        var fileInfo = new FileInfo(file);
                        totalSize += fileInfo.Length;
                        fileInfo.Delete();
                    }
                    catch
                    {
                        // Ignorar archivos en uso
                    }
                }
            }
            catch
            {
                // Ignorar errores de acceso
            }

            return totalSize;
        }

        #endregion
    }
}
