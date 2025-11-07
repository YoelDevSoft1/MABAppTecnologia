<#
MAB INGENIERIA DE VALOR - OPTIMIZACION DE EQUIPOS (Windows 11 Pro)
Debloat avanzado + Optimizaciones Sistema/SSD/RAM/Avanzado con GUI WPF + Modo Inteligente (Auto).

- No deshabilita Defender, BitLocker ni Windows Update.
- Perfil profundo MAB por defecto (Drive + ArcGIS + Office).
- Logs robustos (StreamWriter) y Transcript separado.
- Punto de restauracion sin espera de 24h (opcional, incluido).
- Win11 Bypass toggle (On/Off + extras).
- Perfiles inteligentes: detecta RAM, SSD, laptop/desktop/VM, espacio libre, etc.
#>

function Ensure-STA {
  if ([System.Threading.Thread]::CurrentThread.ApartmentState -ne 'STA') {
    $scriptPath = $PSCommandPath; if (-not $scriptPath) { $scriptPath = $MyInvocation.MyCommand.Path }
    $shell = (Get-Process -Id $PID).Path
    $args  = "-STA -ExecutionPolicy Bypass -File `"$scriptPath`""
    Start-Process -FilePath $shell -ArgumentList $args -Verb RunAs
    exit
  }
}
Ensure-STA

Add-Type -AssemblyName System.Xaml, PresentationFramework, PresentationCore, WindowsBase

function Require-Admin {
  $isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).
    IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
  if (-not $isAdmin) {
    [System.Windows.MessageBox]::Show("Ejecuta PowerShell como Administrador.","MAB Ingenieria de Valor",
      [System.Windows.MessageBoxButton]::OK,[System.Windows.MessageBoxImage]::Warning) | Out-Null
    throw "Se requieren permisos de Administrador."
  }
}
Require-Admin

# ===== Rutas / estado =====
$RunId          = (Get-Date).ToString('yyyyMMdd_HHmmss')
$Base           = "$env:ProgramData\MAB_OPT"
$RegBackup      = Join-Path $Base "reg_$RunId"
$PkgLog         = Join-Path $Base "removed_packages_$RunId.csv"
$ProvLog        = Join-Path $Base "removed_provisioned_$RunId.csv"
$LogFile        = Join-Path $Base "opt_$RunId.log"
$TranscriptFile = Join-Path $Base "transcript_$RunId.log"
$StateFile      = Join-Path $Base "state_$RunId.json"
New-Item -ItemType Directory -Force -Path $Base, $RegBackup | Out-Null

$Script:DryRun = $false
$Global:State  = [ordered]@{ Services=@(); Hibernation=$null; PagefileAuto=$null }

# ===== Logging robusto (StreamWriter) =====
$Script:LogWriter = $null
function Open-LogWriter {
  try {
    $Script:LogWriter = New-Object System.IO.StreamWriter($LogFile, $true, [System.Text.Encoding]::UTF8)
    $Script:LogWriter.AutoFlush = $true
  } catch {}
}
Open-LogWriter

function Write-Log([string]$Message){
  $stamp = (Get-Date).ToString('HH:mm:ss')
  $line = "[$stamp] $Message"
  if ($global:txtLog -and $global:txtLog -is [System.Windows.Controls.TextBox]) {
    $global:txtLog.AppendText($line + "`r`n"); $global:txtLog.ScrollToEnd()
  }
  try {
    if ($Script:LogWriter) { $Script:LogWriter.WriteLine($line) }
    else { Add-Content -Path $LogFile -Value $line -ErrorAction SilentlyContinue }
  } catch {
    Start-Sleep -Milliseconds 150
    try { Add-Content -Path $LogFile -Value $line -ErrorAction SilentlyContinue } catch {}
  }
}

# ===== Utilidades =====
function PSPathToReg([string]$Path){ $p=$Path.Trim(); $p=$p -replace '^HKLM:\\','HKEY_LOCAL_MACHINE\'; $p=$p -replace '^HKCU:\\','HKEY_CURRENT_USER\'; return $p }
function Backup-RegKey([string]$KeyPath){ try{ $regPath=PSPathToReg $KeyPath; $safe=($regPath -replace '[\\/:*?"<>|]','_'); $out=Join-Path $RegBackup "$safe.reg"; reg.exe export "$regPath" "$out" /y | Out-Null }catch{} }
function Set-Reg {
  param([Parameter(Mandatory)][string]$Path,[Parameter(Mandatory)][string]$Name,
        [Parameter(Mandatory)][ValidateSet('DWord','QWord','String','ExpandString','MultiString')][string]$Type,[Parameter(Mandatory)]$Value)
  if ($Script:DryRun){ Write-Log ("DRY-RUN: Set {0}\{1} ({2}) = {3}" -f $Path,$Name,$Type,$Value); return }
  if (-not (Test-Path $Path)) { New-Item -Path $Path -Force | Out-Null }
  Backup-RegKey -KeyPath $Path
  New-ItemProperty -Path $Path -Name $Name -PropertyType $Type -Value $Value -Force | Out-Null
  Write-Log ("Set {0}\{1} = {2}" -f $Path,$Name,$Value)
}
function Remove-RegValue {
  param([Parameter(Mandatory)][string]$Path,[Parameter(Mandatory)][string]$Name)
  if ($Script:DryRun){ Write-Log ("DRY-RUN: Remove {0}\{1}" -f $Path,$Name); return }
  try { if (Test-Path $Path) { Remove-ItemProperty -Path $Path -Name $Name -Force -ErrorAction Stop; Write-Log ("Del {0}\{1}" -f $Path,$Name) } } catch {}
}
function Save-ServiceState([string]$Name){ try{ $startup=(Get-CimInstance -ClassName Win32_Service -Filter "Name='$Name'").StartMode; $Global:State.Services += [ordered]@{Name=$Name;StartMode=$startup} }catch{} }
function Set-ServiceStart([string]$Name,[string]$Mode){
  if ($Script:DryRun){ Write-Log ("DRY-RUN: Set-Service {0} -> {1}" -f $Name,$Mode); return }
  try { Set-Service -Name $Name -StartupType $Mode; Write-Log ("Servicio {0} -> {1}" -f $Name,$Mode) }
  catch { Write-Log ("No se pudo {0} -> {1}: {2}" -f $Name,$Mode,$_.Exception.Message) }
}
function Save-StateToFile { $Global:State | ConvertTo-Json -Depth 5 | Set-Content -Path $StateFile -Encoding UTF8 }

# ===== Sistema =====
function Module-Privacy {
  Write-Log "Privacidad y anuncios"
  Set-Reg -Path "HKLM:\SOFTWARE\Policies\Microsoft\Windows\DataCollection" -Name "AllowTelemetry" -Type DWord -Value 0
  Set-Reg -Path "HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\ContentDeliveryManager" -Name "SystemPaneSuggestionsEnabled" -Type DWord -Value 0
  "538387","310093","338388","314559","353694","353696" | ForEach-Object {
    Set-Reg -Path "HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\ContentDeliveryManager" -Name ("SubscribedContent-{0}Enabled" -f $_) -Type DWord -Value 0
  }
  Set-Reg -Path "HKLM:\SOFTWARE\Policies\Microsoft\Windows\CloudContent" -Name "DisableConsumerFeatures" -Type DWord -Value 1
  Set-Reg -Path "HKCU:\SOFTWARE\Policies\Microsoft\Windows\CloudContent" -Name "DisableSoftLanding" -Type DWord -Value 1
  Set-Reg -Path "HKCU:\SOFTWARE\Policies\Microsoft\Windows\CloudContent" -Name "DisableWindowsSpotlightFeatures" -Type DWord -Value 1
  Set-Reg -Path "HKCU:\Software\Policies\Microsoft\Windows\Explorer" -Name "DisableSearchBoxSuggestions" -Type DWord -Value 1
  Set-Reg -Path "HKCU:\Software\Policies\Microsoft\Windows\WindowsCopilot" -Name "TurnOffWindowsCopilot" -Type DWord -Value 1
  New-Item -Path "HKCU:\Software\Policies\Microsoft\Windows\Windows Search" -Force | Out-Null
  Set-Reg -Path "HKCU:\Software\Policies\Microsoft\Windows\Windows Search" -Name "EnableDynamicContentInWSB" -Type DWord -Value 0
}
function Module-Performance {
  Write-Log "Rendimiento visual/energia"
  Set-Reg -Path "HKLM:\SOFTWARE\Policies\Microsoft\Windows\AppPrivacy" -Name "LetAppsRunInBackground" -Type DWord -Value 2
  Set-Reg -Path "HKCU:\Software\Microsoft\Windows\CurrentVersion\BackgroundAccessApplications" -Name "GlobalUserDisabled" -Type DWord -Value 1
  try{
    $hp = (powercfg -list) | Select-String -Pattern '(?i)high\s*performance|alto\s*rendimiento'
    if($hp){
      $guid = (($hp.Line | Select-String -Pattern '[0-9a-fA-F]{8}-([0-9a-fA-F]{4}-){3}[0-9a-fA-F]{12}').Matches.Value | Select-Object -First 1)
      if($guid){
        if($Script:DryRun){Write-Log ("DRY-RUN: powercfg -setactive {0}" -f $guid)} else { powercfg -setactive $guid | Out-Null; Write-Log "Plan de energia: High performance" }
      }
    }
  }catch{}
  Set-Reg -Path "HKCU:\Control Panel\Desktop\WindowMetrics" -Name "MinAnimate" -Type String -Value "0"
  Set-Reg -Path "HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects" -Name "VisualFXSetting" -Type DWord -Value 2
  New-Item -Path "HKCU:\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize" -Force | Out-Null
  Set-Reg -Path "HKCU:\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize" -Name "EnableTransparency" -Type DWord -Value 0
  Set-Reg -Path "HKCU:\Software\Microsoft\Windows\CurrentVersion\StorageSense\Parameters\StoragePolicy" -Name "01" -Type DWord -Value 1
  New-Item -Path "HKLM:\SOFTWARE\Policies\Microsoft\Windows\GameDVR" -Force | Out-Null
  Set-Reg -Path "HKLM:\SOFTWARE\Policies\Microsoft\Windows\GameDVR" -Name "AllowGameDVR" -Type DWord -Value 0
}
function Module-UX {
  Write-Log "UX limpio (Widgets/Chat/Edge prelaunch)"
  Set-Reg -Path "HKLM:\SOFTWARE\Policies\Microsoft\Dsh" -Name "AllowWidgets" -Type DWord -Value 0
  Set-Reg -Path "HKLM:\SOFTWARE\Policies\Microsoft\Windows\Windows Chat" -Name "ChatIcon" -Type DWord -Value 3
  Set-Reg -Path "HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced" -Name "Start_IrisRecommendations" -Type DWord -Value 0
  Set-Reg -Path "HKLM:\SOFTWARE\Policies\Microsoft\MicrosoftEdge\Main" -Name "AllowPrelaunch" -Type DWord -Value 0
  Set-Reg -Path "HKLM:\SOFTWARE\Policies\Microsoft\MicrosoftEdge\TabPreloader" -Name "AllowTabPreloading" -Type DWord -Value 0
}
function Module-Update {
  Write-Log "Windows Update prudente + Delivery Optimization HTTP"
  Set-Reg -Path "HKLM:\SOFTWARE\Microsoft\WindowsUpdate\UX\Settings" -Name "ActiveHoursStart" -Type DWord -Value 8
  Set-Reg -Path "HKLM:\SOFTWARE\Microsoft\WindowsUpdate\UX\Settings" -Name "ActiveHoursEnd" -Type DWord -Value 18
  Set-Reg -Path "HKLM:\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate" -Name "DeferFeatureUpdatesPeriodInDays" -Type DWord -Value 14
  Set-Reg -Path "HKLM:\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate" -Name "DeferQualityUpdatesPeriodInDays" -Type DWord -Value 7
  Set-Reg -Path "HKLM:\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU" -Name "NoAutoRebootWithLoggedOnUsers" -Type DWord -Value 1
  New-Item -Path "HKLM:\SOFTWARE\Policies\Microsoft\Windows\DeliveryOptimization" -Force | Out-Null
  Set-Reg -Path "HKLM:\SOFTWARE\Policies\Microsoft\Windows\DeliveryOptimization" -Name "DODownloadMode" -Type DWord -Value 0
}
function Harden-TelemetryServices {
  Write-Log "Endureciendo servicios/tareas de telemetria"
  foreach($svc in @("DiagTrack","dmwappushservice","RetailDemo")){ Save-ServiceState -Name $svc; Set-ServiceStart -Name $svc -Mode "Disabled" }
  $tasks = @(
    "\Microsoft\Windows\Feedback\Siuf\DmClient",
    "\Microsoft\Windows\Feedback\Siuf\DmClientOnScenarioDownload",
    "\Microsoft\Windows\Maps\MapsToastTask",
    "\Microsoft\Windows\Maps\MapsUpdateTask"
  )
  foreach($t in $tasks){
    try{
      $parts = $t.Split('\'); $name = $parts[-1]; $path = ($t -replace "\\$name$","\") 
      if(-not $Script:DryRun){ Disable-ScheduledTask -TaskPath $path -TaskName $name -ErrorAction SilentlyContinue | Out-Null }
      Write-Log ("Tarea deshabilitada: {0}" -f $t)
    }catch{}
  }
}

# ===== Debloat =====
$PreserveCommon = @(
  "Microsoft.StorePurchaseApp","Microsoft.WindowsStore",
  "Microsoft.VCLibs","Microsoft.NET.Native.Runtime","Microsoft.NET.Native.Framework",
  "Microsoft.DesktopAppInstaller","Microsoft.UI.Xaml","Microsoft.WindowsAppRuntime.1.5",
  "Microsoft.HEIFImageExtension","Microsoft.VP9VideoExtensions","Microsoft.WebMediaExtensions",
  "Microsoft.AAD.BrokerPlugin","Microsoft.Edge",
  "Microsoft.OfficeHub","Microsoft.Outlook","MSTeams","Microsoft.Teams",
  "MicrosoftCorporationII.QuickAssist"
)
function Invoke-AppxRemoval {
  param([string[]]$RemoveList,[string[]]$PreserveList,[string]$Tag="std")
  $pkgs = Get-AppxPackage -AllUsers
  $toRemove = $pkgs | Where-Object { ($RemoveList -contains $_.Name) -and -not ($PreserveList -contains $_.Name) }
  if ($toRemove.Count -eq 0) { Write-Log ("No hay paquetes para remover ({0})." -f $Tag); return }
  $log = @()
  foreach ($p in $toRemove) {
    if ($Script:DryRun) { Write-Log ("DRY-RUN: Remove-AppxPackage {0}" -f $p.Name); continue }
    try {
      Remove-AppxPackage -AllUsers -Package $p.PackageFullName -ErrorAction Stop
      Write-Log ("Removido: {0}" -f $p.Name)
      $log += [pscustomobject]@{ Name=$p.Name; PackageFullName=$p.PackageFullName; Time=(Get-Date) }
    } catch { Write-Log ("No se pudo remover {0}: {1}" -f $p.Name, $_.Exception.Message) }
  }
  if (-not $Script:DryRun -and $log.Count -gt 0) { $log | Export-Csv -Path $PkgLog -NoTypeInformation -Encoding UTF8; Write-Log ("CSV: {0}" -f $PkgLog) }
}
function Remove-Provisioned {
  param([string[]]$Names,[string]$Tag)
  if ($Script:DryRun){ Write-Log ("DRY-RUN: se removerian provisionados ({0})" -f $Tag); return }
  $prov = Get-AppxProvisionedPackage -Online | Where-Object { $Names -contains $_.DisplayName }
  if (-not $prov){ Write-Log "Provisionados a remover: 0"; return }
  $logProv=@()
  foreach($p in $prov){
    try{ Remove-AppxProvisionedPackage -Online -PackageName $p.PackageName -ErrorAction Stop | Out-Null
         Write-Log ("Provisioned removido: {0}" -f $p.DisplayName)
         $logProv += [pscustomobject]@{ DisplayName=$p.DisplayName; PackageName=$p.PackageName; Time=(Get-Date) } }
    catch{ Write-Log ("Provisioned error {0}: {1}" -f $p.DisplayName, $_.Exception.Message) }
  }
  if ($logProv.Count -gt 0){ $logProv | Export-Csv -Path $ProvLog -NoTypeInformation -Encoding UTF8; Write-Log ("CSV provisionados: {0}" -f $ProvLog) }
}
function Remove-CapabilitiesSafe {
  Write-Log "Quitando caracteristicas opcionales comunes no usadas"
  $caps = @(
    "XPS.Viewer~~~~0.0.1.0","MathRecognizer~~~~0.0.1.0","Microsoft-Windows-TabletPCMath~~~~0.0.1.0",
    "Hello-Face~~~~0.0.1.0","Print.Fax.Scan~~~~0.0.1.0","WorkFolders-Client~~~~0.0.1.0"
  )
  foreach($c in $caps){
    try{
      if ($Script:DryRun){ Write-Log ("DRY-RUN: Remove-WindowsCapability {0}" -f $c) }
      else { Remove-WindowsCapability -Online -Name $c -ErrorAction Stop | Out-Null; Write-Log ("Capability removida: {0}" -f $c) }
    } catch { Write-Log ("Capability {0} no aplicada: {1}" -f $c, $_.Exception.Message) }
  }
}
function Module-DebloatStandard {
  Write-Log "Debloat estandar"
  $Remove = @(
    "Microsoft.BingNews","Microsoft.BingWeather","Microsoft.GamingApp",
    "Microsoft.GetHelp","Microsoft.Getstarted","Microsoft.Microsoft3DViewer",
    "Microsoft.MicrosoftSolitaireCollection","Microsoft.MixedReality.Portal",
    "Microsoft.People","Microsoft.SkypeApp","Microsoft.Todos",
    "Microsoft.Wallet","Microsoft.OneConnect","Microsoft.WindowsMaps",
    "Microsoft.XboxApp","Microsoft.XboxSpeechToTextOverlay","Microsoft.Xbox.TCUI",
    "Microsoft.XboxGamingOverlay","Microsoft.XboxGameOverlay",
    "Microsoft.ZuneMusic","Microsoft.ZuneVideo","Clipchamp.Clipchamp","BytedancePte.Ltd.TikTok",
    "Microsoft.549981C3F5F10"
  )
  Invoke-AppxRemoval -RemoveList $Remove -PreserveList $PreserveCommon -Tag "std"
}
function Module-DebloatDeepMAB {
  Write-Log "Debloat profundo - Perfil MAB (Drive + ArcGIS + Office)"
  $Remove = @(
    "Microsoft.GamingApp","Microsoft.XboxApp","Microsoft.Xbox.TCUI","Microsoft.XboxGameOverlay","Microsoft.XboxGamingOverlay","Microsoft.XboxSpeechToTextOverlay",
    "Microsoft.ZuneMusic","Microsoft.ZuneVideo","Microsoft.Microsoft3DViewer","Microsoft.SkypeApp","Microsoft.Todos","Microsoft.People",
    "Microsoft.BingNews","Microsoft.BingWeather","Microsoft.GetHelp","Microsoft.Getstarted","Microsoft.MixedReality.Portal","Microsoft.WindowsMaps",
    "Microsoft.Whiteboard","Microsoft.WindowsFeedbackHub","Microsoft.YourPhone",
    "Clipchamp.Clipchamp","BytedancePte.Ltd.TikTok","Microsoft.Copilot",
    "Microsoft.549981C3F5F10",
    "MicrosoftWindows.Client.WebExperience"
  )
  $OptionalRemove = @("Microsoft.Paint","Microsoft.MSPaint","Microsoft.StickyNotes","Microsoft.Photos","Microsoft.WindowsSoundRecorder","Microsoft.WindowsCamera")
  $AllRemove = $Remove + $OptionalRemove
  Invoke-AppxRemoval -RemoveList $AllRemove -PreserveList $PreserveCommon -Tag "deep"
  Remove-Provisioned -Names $AllRemove -Tag "deep"
  Remove-CapabilitiesSafe
}
function Remove-OneDrive {
  Write-Log "Quitar OneDrive"
  if ($Script:DryRun) { Write-Log "DRY-RUN: OneDriveSetup.exe /uninstall + politicas"; return }
  try {
    Get-Process OneDrive -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
    $od = "$env:SystemRoot\SysWOW64\OneDriveSetup.exe"; if (-not (Test-Path $od)) { $od = "$env:SystemRoot\System32\OneDriveSetup.exe" }
    if (Test-Path $od) { Start-Process $od "/uninstall" -Wait; Write-Log "OneDrive desinstalado." } else { Write-Log "OneDriveSetup no encontrado." }
    New-Item -Path "HKLM:\Software\Policies\Microsoft\Windows\OneDrive" -Force | Out-Null
    Set-Reg -Path "HKLM:\Software\Policies\Microsoft\Windows\OneDrive" -Name "DisableFileSyncNGSC" -Type DWord -Value 1
  } catch { Write-Log ("OneDrive: {0}" -f $_.Exception.Message) }
}

# ===== SSD =====
function SSD-EnsureTRIM { Write-Log "SSD: habilitar TRIM"; if ($Script:DryRun){Write-Log "DRY-RUN: fsutil behavior set DisableDeleteNotify 0"} else { try { fsutil behavior set DisableDeleteNotify 0 | Out-Null; Write-Log "TRIM habilitado." } catch { Write-Log ("TRIM: {0}" -f $_.Exception.Message) } } }
function SSD-ReTrimAll {
  Write-Log "SSD: ReTrim en volumenes NTFS/ReFS"
  $vols = Get-Volume | Where-Object { $_.DriveType -eq 'Fixed' -and ($_.FileSystem -in @('NTFS','ReFS')) -and $_.DriveLetter }
  foreach ($v in $vols) {
    if ($Script:DryRun) { Write-Log ("DRY-RUN: Optimize-Volume -DriveLetter {0} -ReTrim" -f $v.DriveLetter) }
    else { try { Optimize-Volume -DriveLetter $v.DriveLetter -ReTrim -Verbose -ErrorAction Stop | Out-Null; Write-Log ("ReTrim: {0}" -f $v.DriveLetter) } catch { Write-Log ("ReTrim {0}: {1}" -f $v.DriveLetter, $_.Exception.Message) } }
  }
}
function SSD-DisableLastAccess { Write-Log "SSD: desactivar 'Ultimo acceso'"; if ($Script:DryRun){Write-Log "DRY-RUN: fsutil behavior set disablelastaccess 1"} else { try { fsutil behavior set disablelastaccess 1 | Out-Null; Write-Log "LastAccess desactivado." } catch { Write-Log ("LastAccess: {0}" -f $_.Exception.Message) } } }
function SSD-EnableOptimizeTask {
  Write-Log "SSD: habilitar tarea 'Optimizar Unidades'"
  try{ if ($Script:DryRun){ Write-Log "DRY-RUN: Enable-ScheduledTask ScheduledDefrag" } else { Enable-ScheduledTask -TaskPath '\Microsoft\Windows\Defrag\' -TaskName 'ScheduledDefrag' | Out-Null }; Write-Log "Tarea habilitada." }
  catch{ Write-Log ("ScheduledDefrag: {0}" -f $_.Exception.Message) }
}
function SSD-RemoveHibernation {
  Write-Log "SSD: quitar hibernacion"
  try{
    $hibKey="HKLM:\SYSTEM\CurrentControlSet\Control\Power"
    $val=(Get-ItemProperty -Path $hibKey -Name HibernateEnabled -ErrorAction SilentlyContinue).HibernateEnabled
    $Global:State.Hibernation = $(if ($null -eq $val){$null}else{[int]$val})
  }catch{}
  if ($Script:DryRun){ Write-Log "DRY-RUN: powercfg -h off"; return }
  try{ powercfg -h off | Out-Null; Write-Log "Hibernacion deshabilitada." }catch{ Write-Log ("Hibernacion: {0}" -f $_.Exception.Message) }
}

# ===== RAM =====
function RAM-EnableMemoryCompression { Write-Log "RAM: habilitar Memory Compression"; if ($Script:DryRun){Write-Log "DRY-RUN: Enable-MMAgent -MemoryCompression"} else { try { Enable-MMAgent -MemoryCompression | Out-Null; Write-Log "Memory Compression activo." } catch { Write-Log ("Memory Compression: {0}" -f $_.Exception.Message) } } }
function RAM-SetPagefileAuto {
  Write-Log "RAM: pagefile administrado por el sistema"
  try{ $cs = Get-CimInstance -ClassName Win32_ComputerSystem -ErrorAction Stop; $Global:State.PagefileAuto = $cs.AutomaticManagedPagefile }catch{}
  if ($Script:DryRun){ Write-Log "DRY-RUN: Set AutomaticManagedPagefile=True + limpiar tamanos fijos"; return }
  try{
    Get-CimInstance -ClassName Win32_PageFileSetting -ErrorAction SilentlyContinue | Remove-CimInstance -ErrorAction SilentlyContinue
    $cs = Get-CimInstance -ClassName Win32_ComputerSystem -ErrorAction Stop
    Set-CimInstance -InputObject $cs -Property @{ AutomaticManagedPagefile = $true } | Out-Null
    Write-Log "Pagefile en automatico."
  }catch{ Write-Log ("Pagefile: {0}" -f $_.Exception.Message) }
}
function RAM-StrictMode {
  Write-Log "RAM (estricto): SysMain Disabled + WSearch Manual"
  foreach($n in @("SysMain","WSearch")){ Save-ServiceState -Name $n }
  Set-ServiceStart -Name "SysMain" -Mode "Disabled"
  Set-ServiceStart -Name "WSearch" -Mode "Manual"
  if (-not $Script:DryRun){ foreach($n in @("SysMain","WSearch")){ try{ Get-Service $n -ErrorAction Stop | Stop-Service -Force -ErrorAction SilentlyContinue }catch{} } }
  Write-Log "Modo estricto aplicado."
}

# ===== Avanzado =====
function Advanced-UltimatePerformance {
  Write-Log "Avanzado: Ultimate Performance (desktop)"
  try {
    $isLaptop = $false
    try {
      $ch = (Get-CimInstance Win32_SystemEnclosure -ErrorAction SilentlyContinue).ChassisTypes
      if ($ch -and ($ch -contains 8 -or $ch -contains 9 -or $ch -contains 10 -or $ch -contains 14)) { $isLaptop = $true }
    } catch {}
    if ($isLaptop) { Write-Log "Portatil detectado: se omite Ultimate Performance."; return }

    if ($Script:DryRun) { Write-Log "DRY-RUN: crear/activar plan Ultimate Performance"; return }

    $baseGuid = "e9a42b02-d5df-448d-aa00-03f14749eb61"
    $dupOut = & powercfg -duplicatescheme $baseGuid 2>&1

    $guidRegex = '[0-9a-fA-F]{8}-([0-9a-fA-F]{4}-){3}[0-9a-fA-F]{12}'
    $newGuid = ($dupOut | Select-String -Pattern $guidRegex -AllMatches | ForEach-Object { $_.Matches.Value } | Select-Object -Last 1)

    if (-not $newGuid) {
      $listRaw = & powercfg -list 2>$null
      $nameHit = $listRaw | Select-String -Pattern '(?i)ultimate|maxim[o|a]\s*rendimiento|rendimiento\s*maxim[o|a]'
      if ($nameHit) {
        $line = $nameHit.Line
        $newGuid = ($line | Select-String -Pattern $guidRegex).Matches.Value | Select-Object -First 1
      }
      if (-not $newGuid) {
        $guidHit = $listRaw | Select-String -Pattern $baseGuid -SimpleMatch
        if ($guidHit) { $newGuid = $baseGuid }
      }
    }

    if (-not $newGuid) {
      Write-Log "No se pudo determinar GUID de Ultimate. Activando High Performance como alternativa."
      $hp = (& powercfg -list) | Select-String -Pattern '(?i)high\s*performance|alto\s*rendimiento'
      if ($hp) {
        $hpGuid = (($hp.Line | Select-String -Pattern $guidRegex).Matches.Value | Select-Object -First 1)
        if ($hpGuid) { & powercfg -setactive $hpGuid 2>$null; Write-Log ("Activado High Performance: {0}" -f $hpGuid) }
      }
      return
    }

    $newGuid = $newGuid.Trim('{}').Trim()
    $rc = & powercfg -setactive $newGuid 2>&1
    if ($LASTEXITCODE -ne 0) {
      Write-Log ("powercfg -setactive fallo: {0}" -f ($rc | Out-String).Trim())
      $hp = (& powercfg -list) | Select-String -Pattern '(?i)high\s*performance|alto\s*rendimiento'
      if ($hp) {
        $hpGuid = (($hp.Line | Select-String -Pattern $guidRegex).Matches.Value | Select-Object -First 1)
        if ($hpGuid) { & powercfg -setactive $hpGuid 2>$null; Write-Log ("Activado High Performance: {0}" -f $hpGuid) }
      }
    } else {
      Write-Log ("Ultimate Performance activo: {0}" -f $newGuid)
    }
  } catch {
    Write-Log ("Ultimate Performance error: {0}" -f $_.Exception.Message)
  }
}
function Advanced-GPUHags {
  Write-Log "Avanzado: HAGS (GPU hardware scheduling)"
  Set-Reg -Path "HKLM:\SYSTEM\CurrentControlSet\Control\GraphicsDrivers" -Name "HwSchMode" -Type DWord -Value 2
  Write-Log "HAGS configurado (requiere reinicio si la GPU/driver lo soporta)."
}
function Advanced-NTFS83 {
  Write-Log "Avanzado: NTFS 8.3 OFF en volumenes fijos"
  try { if ($Script:DryRun) { Write-Log "DRY-RUN: fsutil 8dot3name set 2" } else { fsutil 8dot3name set 2 | Out-Null } } catch { Write-Log ("8.3 default: {0}" -f $_.Exception.Message) }
  $vols = Get-Volume | Where-Object { $_.DriveType -eq 'Fixed' -and $_.DriveLetter }
  foreach ($v in $vols) {
    try {
      if ($Script:DryRun) { Write-Log ("DRY-RUN: fsutil 8dot3name set {0}: 1" -f $v.DriveLetter) }
      else { fsutil 8dot3name set "$($v.DriveLetter):" 1 | Out-Null; Write-Log ("8.3 OFF en {0}:" -f $v.DriveLetter) }
    } catch { Write-Log ("8.3 en {0}: {1}" -f $v.DriveLetter, $_.Exception.Message) }
  }
  Write-Log "Nota: algunos instaladores legacy requieren 8.3."
}
function Advanced-StartupPrune {
  Write-Log "Avanzado: Podar Autostart (Run/Run32) con whitelist"
  $whitelist = @("GoogleDriveFS","GoogleDrive","OneDrive","Teams","MSTeams","Outlook","OfficeClickToRun")
  $roots = @("HKCU:\Software\Microsoft\Windows\CurrentVersion\Run","HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Run")
  foreach ($root in $roots) {
    try {
      Backup-RegKey -KeyPath $root
      $disabled = "$root DisabledByMAB"
      if (-not $Script:DryRun) { New-Item -Path $disabled -Force | Out-Null }
      $props = (Get-ItemProperty -Path $root -ErrorAction SilentlyContinue)
      if ($null -eq $props) { continue }
      $names = $props.PSObject.Properties | Where-Object { $_.Name -notin "PSPath","PSParentPath","PSChildName","PSDrive","PSProvider" } | Select-Object -ExpandProperty Name
      foreach ($n in $names) {
        if ($whitelist -notcontains $n) {
          $val = (Get-ItemPropertyValue -Path $root -Name $n -ErrorAction SilentlyContinue)
          if ($Script:DryRun) { Write-Log ("DRY-RUN: mover {0}\{1} a {2}" -f $root,$n,$disabled) }
          else {
            if ($null -ne $val) { New-ItemProperty -Path $disabled -Name $n -Value $val -PropertyType String -Force | Out-Null }
            Remove-ItemProperty -Path $root -Name $n -Force -ErrorAction SilentlyContinue
            Write-Log ("Deshabilitado autostart: {0} ({1})" -f $n,$root)
          }
        }
      }
    } catch { Write-Log ("Startup prune {0}: {1}" -f $root, $_.Exception.Message) }
  }
}
function Advanced-ScheduleReTrim {
  Write-Log "Avanzado: Tarea programada ReTrim mensual (1er dia, 02:00)"
  $taskPath = "\MAB\"
  $taskName = "MonthlyReTrim"
  $psCmd = "Get-Volume | Where-Object { `$_.DriveType -eq 'Fixed' -and (`$_.FileSystem -eq 'NTFS' -or `$_.FileSystem -eq 'ReFS') -and `$_.DriveLetter } | ForEach-Object { Optimize-Volume -DriveLetter `$_.DriveLetter -ReTrim }"
  if ($Script:DryRun) { Write-Log ("DRY-RUN: Register-ScheduledTask {0}{1}" -f $taskPath,$taskName); return }
  try {
    $act = New-ScheduledTaskAction -Execute "powershell.exe" -Argument "-NoProfile -WindowStyle Hidden -Command `"$psCmd`""
    $trg = New-ScheduledTaskTrigger -Monthly -DaysOfMonth 1 -At 02:00
    Register-ScheduledTask -TaskPath $taskPath -TaskName $taskName -Action $act -Trigger $trg -RunLevel Highest -Force | Out-Null
    Write-Log "Tarea creada/actualizada."
  } catch { Write-Log ("Scheduled ReTrim: {0}" -f $_.Exception.Message) }
}
function Advanced-ComponentCleanup {
  param([switch]$Aggressive)
  Write-Log ("Avanzado: DISM StartComponentCleanup{0}" -f ($(if($Aggressive){" + ResetBase (agresivo)"} else {""})))
  if ($Script:DryRun) { Write-Log "DRY-RUN: DISM /Online /Cleanup-Image /StartComponentCleanup"; if ($Aggressive){Write-Log "DRY-RUN: DISM /Online /Cleanup-Image /StartComponentCleanup /ResetBase"}; return }
  try {
    DISM /Online /Cleanup-Image /StartComponentCleanup /Quiet | Out-Null
    if ($Aggressive) { DISM /Online /Cleanup-Image /StartComponentCleanup /ResetBase /Quiet | Out-Null }
    Write-Log "DISM limpieza de componentes realizada."
  } catch { Write-Log ("DISM StartComponentCleanup: {0}" -f $_.Exception.Message) }
}
function Advanced-HealthCheck {
  Write-Log "Avanzado: DISM /RestoreHealth + SFC /scannow"
  if ($Script:DryRun) { Write-Log "DRY-RUN: DISM /Online /Cleanup-Image /RestoreHealth ; sfc /scannow"; return }
  try { DISM /Online /Cleanup-Image /RestoreHealth | Out-Null; Write-Log "DISM /RestoreHealth OK." } catch { Write-Log ("DISM /RestoreHealth: {0}" -f $_.Exception.Message) }
  try { sfc /scannow | Out-Null; Write-Log "SFC OK (ver CBS.log si hubo reparaciones)." } catch { Write-Log ("SFC: {0}" -f $_.Exception.Message) }
}
function Advanced-PowerThrottlingOff {
  Write-Log "Avanzado: Power Throttling OFF (global)"
  New-Item -Path "HKLM:\SYSTEM\CurrentControlSet\Control\Power\PowerThrottling" -Force | Out-Null
  Set-Reg -Path "HKLM:\SYSTEM\CurrentControlSet\Control\Power\PowerThrottling" -Name "PowerThrottlingOff" -Type DWord -Value 1
}

# ===== Win11 Bypass (toggle) =====
function Get-Win11BypassState {
  $mo = Get-ItemProperty -Path "HKLM:\SYSTEM\Setup\MoSetup" -Name "AllowUpgradesWithUnsupportedTPMOrCPU" -ErrorAction SilentlyContinue
  $lab = Get-ItemProperty -Path "HKLM:\SYSTEM\Setup\LabConfig" -ErrorAction SilentlyContinue
  $enabled = ($mo.AllowUpgradesWithUnsupportedTPMOrCPU -eq 1) -or ($lab.BypassTPMCheck -eq 1 -or $lab.BypassSecureBootCheck -eq 1)
  $extras  = ($lab.BypassCPUCheck -eq 1 -or $lab.BypassRAMCheck -eq 1 -or $lab.BypassStorageCheck -eq 1)
  [pscustomobject]@{ Enabled=$enabled; Extras=$extras }
}
function Set-Win11Bypass {
  param([bool]$Enable,[bool]$IncludeExtras)
  Write-Log ("Win11 Bypass -> " + ($(if($Enable){"ON"}else{"OFF"})) + ($(if($IncludeExtras){" + extras"}{" "})))
  Backup-RegKey "HKLM:\SYSTEM\Setup\MoSetup"
  Backup-RegKey "HKLM:\SYSTEM\Setup\LabConfig"
  if ($Enable) {
    New-Item -Path "HKLM:\SYSTEM\Setup\MoSetup" -Force | Out-Null
    Set-Reg -Path "HKLM:\SYSTEM\Setup\MoSetup" -Name "AllowUpgradesWithUnsupportedTPMOrCPU" -Type DWord -Value 1
    New-Item -Path "HKLM:\SYSTEM\Setup\LabConfig" -Force | Out-Null
    Set-Reg -Path "HKLM:\SYSTEM\Setup\LabConfig" -Name "BypassTPMCheck" -Type DWord -Value 1
    Set-Reg -Path "HKLM:\SYSTEM\Setup\LabConfig" -Name "BypassSecureBootCheck" -Type DWord -Value 1
    if ($IncludeExtras) {
      Set-Reg -Path "HKLM:\SYSTEM\Setup\LabConfig" -Name "BypassCPUCheck" -Type DWord -Value 1
      Set-Reg -Path "HKLM:\SYSTEM\Setup\LabConfig" -Name "BypassRAMCheck" -Type DWord -Value 1
      Set-Reg -Path "HKLM:\SYSTEM\Setup\LabConfig" -Name "BypassStorageCheck" -Type DWord -Value 1
    }
  } else {
    Remove-RegValue -Path "HKLM:\SYSTEM\Setup\MoSetup"  -Name "AllowUpgradesWithUnsupportedTPMOrCPU"
    Remove-RegValue -Path "HKLM:\SYSTEM\Setup\LabConfig" -Name "BypassTPMCheck"
    Remove-RegValue -Path "HKLM:\SYSTEM\Setup\LabConfig" -Name "BypassSecureBootCheck"
    if ($IncludeExtras) {
      Remove-RegValue -Path "HKLM:\SYSTEM\Setup\LabConfig" -Name "BypassCPUCheck"
      Remove-RegValue -Path "HKLM:\SYSTEM\Setup\LabConfig" -Name "BypassRAMCheck"
      Remove-RegValue -Path "HKLM:\SYSTEM\Setup\LabConfig" -Name "BypassStorageCheck"
    }
  }
  [System.Windows.MessageBox]::Show(
    "Operacion completada. Se recomienda reiniciar antes de lanzar el setup/upgrade.",
    "MAB - Win11 Bypass", [System.Windows.MessageBoxButton]::OK, [System.Windows.MessageBoxImage]::Information
  ) | Out-Null
}

# ===== Modulos corporativos extra =====
function Module-WU-ExcludeDrivers {
  Write-Log "WU: Excluir drivers desde Windows Update (recomendado en empresa)"
  Set-Reg -Path "HKLM:\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate" -Name "ExcludeWUDriversInQualityUpdate" -Type DWord -Value 1
}
function Module-OEMCleanup {
  Write-Log "OEM/trial cleanup (seguro)"
  $patterns = @(
    "McAfee","WildTangent","Dropbox.*Promotion","HP JumpStart","Dell.*Support.*Assist","Dell Digital Delivery",
    "Candy Crush","ExpressVPN","Norton Security","Booking.com","Amazon Assistant"
  )
  $uninstRoots = @(
    "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
    "HKLM:\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
  )
  foreach($root in $uninstRoots){
    Get-ChildItem $root -ErrorAction SilentlyContinue | ForEach-Object {
      $dn = (Get-ItemProperty $_.PsPath -ErrorAction SilentlyContinue).DisplayName
      $su = (Get-ItemProperty $_.PsPath -ErrorAction SilentlyContinue).UninstallString
      if ($dn -and $su) {
        if ($patterns | Where-Object { $dn -match $_ }) {
          Write-Log ("Detectado OEM/trial: {0}" -f $dn)
          if (-not $Script:DryRun) {
            try {
              if ($su -match "msiexec") {
                $args = $su -replace ".*msiexec\.exe",""
                Start-Process "msiexec.exe" "$args /qn /norestart" -Wait
              } else {
                Start-Process "cmd.exe" "/c `"$su`" /S /quiet /qn /norestart" -Wait
              }
              Write-Log ("Desinstalado: {0}" -f $dn)
            } catch { Write-Log ("No se pudo desinstalar {0}: {1}" -f $dn,$_.Exception.Message) }
          } else {
            Write-Log ("DRY-RUN: desinstalaria {0}" -f $dn)
          }
        }
      }
    }
  }
}
function Module-LanguageCleanup {
  Write-Log "Limpieza de idiomas (retener es-ES y en-US)"
  $keep = @("es-ES","en-US")
  try {
    $caps = Get-WindowsCapability -Online | Where-Object { $_.Name -like "Language.*" -and $_.State -eq "Installed" }
    $remove = @()
    foreach($c in $caps){
      $kp = ($c.Name -replace 'Language\.(Basic|Handwriting|OCR|Speech|TextToSpeech)~~~','')
      $isKeep = $false
      foreach($k in $keep){ if ($kp -like "*$k*") { $isKeep = $true; break } }
      if (-not $isKeep) { $remove += $c }
    }
    foreach($c in $remove){
      if ($Script:DryRun) { Write-Log ("DRY-RUN: Remove-WindowsCapability {0}" -f $c.Name) }
      else {
        try { Remove-WindowsCapability -Online -Name $c.Name -ErrorAction Stop | Out-Null; Write-Log ("Idioma/feature removido: {0}" -f $c.Name) }
        catch { Write-Log ("Idioma no removido {0}: {1}" -f $c.Name, $_.Exception.Message) }
      }
    }
  } catch { Write-Log ("LanguageCleanup error: {0}" -f $_.Exception.Message) }
}
# (Opcional) Delivery Optimization Group mode
function Module-DO-GroupMode {
  param([string]$GroupId)
  if (-not $GroupId) { return }
  Write-Log "Delivery Optimization: Group mode"
  New-Item -Path "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\DeliveryOptimization\Config" -Force | Out-Null
  Set-Reg -Path "HKLM:\SOFTWARE\Microsoft\Windows\DeliveryOptimization" -Name "DODownloadMode" -Type DWord -Value 2
  Set-Reg -Path "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\DeliveryOptimization\Config" -Name "DOGroupId" -Type String -Value $GroupId
  Set-Reg -Path "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\DeliveryOptimization\Config" -Name "DOGroupIdSource" -Type DWord -Value 1
}

# ===== Restauracion =====
function Create-RestorePoint {
  try {
    New-Item -Path "HKLM:\Software\Microsoft\Windows NT\CurrentVersion\SystemRestore" -Force | Out-Null
    New-ItemProperty -Path "HKLM:\Software\Microsoft\Windows NT\CurrentVersion\SystemRestore" -Name "SystemRestorePointCreationFrequency" -PropertyType DWord -Value 0 -Force | Out-Null
  } catch {}
  try {
    if ($Script:DryRun) { Write-Log "DRY-RUN: Crear punto de restauracion"; return }
    Checkpoint-Computer -Description "MAB_FULL_$RunId" -RestorePointType "MODIFY_SETTINGS" | Out-Null
    Write-Log "Punto de restauracion creado."
  } catch { Write-Log "Punto de restauracion no disponible (politica/VM). Continuando." }
}
function Revert-Changes {
  Write-Log "Iniciando reversion..."
  Get-ChildItem $RegBackup -Filter *.reg -ErrorAction SilentlyContinue | ForEach-Object {
    if ($Script:DryRun) { Write-Log ("DRY-RUN: reg import {0}" -f $_.FullName) }
    else { try { reg.exe import "$($_.FullName)" | Out-Null; Write-Log ("Revertida clave: {0}" -f $_.Name) } catch { Write-Log ("reg import error: {0}" -f $_.Exception.Message) } }
  }
  $stateToUse = $null
  $jsons = Get-ChildItem $Base -Filter "state_*.json" | Sort-Object LastWriteTime -Descending
  if ($jsons) { $stateToUse = (Get-Content $jsons[0].FullName -Raw | ConvertFrom-Json) }
  if ($stateToUse) {
    foreach ($s in $stateToUse.Services) {
      if ($s.Name -and $s.StartMode) {
        $mode = switch ($s.StartMode) { "Auto"{"Automatic"} "Automatic"{"Automatic"} "Manual"{"Manual"} "Disabled"{"Disabled"} default{"Manual"} }
        Set-ServiceStart -Name $s.Name -Mode $mode
      }
    }
    if ($null -ne $stateToUse.Hibernation) {
      if ($Script:DryRun) { Write-Log ("DRY-RUN: powercfg -h {0}" -f [int]$stateToUse.Hibernation) }
      else { try { if ([int]$stateToUse.Hibernation -eq 1) { powercfg -h on | Out-Null } else { powercfg -h off | Out-Null } Write-Log "Hibernacion restaurada." } catch { Write-Log ("Hibernacion (revert): {0}" -f $_.Exception.Message) } }
    }
    if ($null -ne $stateToUse.PagefileAuto) {
      if ($Script:DryRun) { Write-Log ("DRY-RUN: AutomaticManagedPagefile={0}" -f $stateToUse.PagefileAuto) }
      else { try { $cs = Get-CimInstance -ClassName Win32_ComputerSystem; Set-CimInstance -InputObject $cs -Property @{ AutomaticManagedPagefile = [bool]$stateToUse.PagefileAuto } | Out-Null; Write-Log "Pagefile auto restaurado." } catch { Write-Log ("Pagefile (revert): {0}" -f $_.Exception.Message) } }
    }
  } else { Write-Log "No se encontro estado previo; se revirtieron claves de registro." }
  Write-Log "Reversion finalizada. Reinicia para consolidar."
}

# ===== Inteligente (perfilado) =====
function Get-EnvProfile {
  $cs  = Get-CimInstance Win32_ComputerSystem -ErrorAction SilentlyContinue
  $encl = (Get-CimInstance Win32_SystemEnclosure -ErrorAction SilentlyContinue).ChassisTypes
  $vol = Get-Volume | Where-Object { $_.DriveLetter -and $_.DriveType -eq 'Fixed' }
  $cVol = $vol | Where-Object { $_.DriveLetter -eq 'C' }
  $isLaptop = $false; if ($encl -and ($encl -contains 8 -or $encl -contains 9 -or $encl -contains 10 -or $encl -contains 14)) { $isLaptop = $true }
  $isVM = $false
  try {
    $m = (Get-CimInstance Win32_ComputerSystem -ErrorAction SilentlyContinue).Manufacturer
    $model = (Get-CimInstance Win32_ComputerSystem -ErrorAction SilentlyContinue).Model
    if ($m -match 'VMware|VirtualBox|QEMU|KVM|Microsoft Corporation' -or $model -match 'Virtual') { $isVM = $true }
  } catch {}
  $ramGB = if ($cs.TotalPhysicalMemory) { [math]::Round(($cs.TotalPhysicalMemory/1GB),0) } else { $null }
  $freeC = if ($cVol) { [math]::Round(($cVol.SizeRemaining/1GB),0) } else { $null }
  $fsC   = if ($cVol) { $cVol.FileSystem } else { $null }
  $hasSSD = $true
  try {
    $phy = Get-PhysicalDisk -ErrorAction SilentlyContinue
    if ($phy) { $hasSSD = ($phy | Where-Object { $_.MediaType -eq 'SSD' }).Count -gt 0 }
  } catch {}
  $vc  = Get-CimInstance Win32_VideoController -ErrorAction SilentlyContinue
  $hagsCandidate = $false; if ($vc) { $hagsCandidate = ($vc.Count -ge 1) }
  [pscustomobject]@{
    PartOfDomain = $cs.PartOfDomain
    IsLaptop     = $isLaptop
    IsVM         = $isVM
    RAM_GB       = $ramGB
    FreeC_GB     = $freeC
    FileSystemC  = $fsC
    HasSSD       = $hasSSD
    HagsCandidate= $hagsCandidate
  }
}
function Compute-SmartPlan {
  $env = Get-EnvProfile
  $plan = [ordered]@{
    Privacy       = $true
    Perf          = $true
    UX            = $true
    Update        = $true
    DebloatDeep   = $true
    RemoveOneDrive= $true
    SSD_TRIM      = $true
    SSD_LastAcc   = $true
    SSD_Sched     = $true
    SSD_NoHiber   = $false
    RAM_Compress  = $true
    RAM_PageAuto  = $true
    RAM_Strict    = $false
    Adv_Ultimate  = $false
    Adv_HAGS      = $false
    Adv_NTFS83    = $true
    Adv_Startup   = $true
    Adv_ReTrimSch = $true
    Adv_DismClean = $true
    Adv_Health    = $false
    Adv_Throttle  = $true
    WU_NoDrivers  = $true
    OEM_Cleanup   = $true
    Lang_Cleanup  = $true
  }
  if ($env.RAM_GB -le 8) { $plan.RAM_Strict = $true }
  if ($env.FreeC_GB -le 30) { $plan.SSD_NoHiber = $true }
  if (-not $env.IsLaptop) { $plan.Adv_Ultimate = $true }
  if ($env.IsVM) { $plan.Adv_HAGS = $false } else { $plan.Adv_HAGS = $env.HagsCandidate }
  if (-not $env.HasSSD) { $plan.SSD_TRIM = $false; $plan.Adv_ReTrimSch = $false }
  [pscustomobject]$plan
}
function Apply-SmartPlan {
  param([switch]$PreviewOnly)
  $plan = Compute-SmartPlan
  Write-Log ("Smart plan -> " + ($plan | ConvertTo-Json -Depth 3))

  # Volcar a UI
  $cbPrivacy.IsChecked        = $plan.Privacy
  $cbPerf.IsChecked           = $plan.Perf
  $cbUX.IsChecked             = $plan.UX
  $cbUpdate.IsChecked         = $plan.Update
  $rbDebloatStd.IsChecked     = $false
  $rbDebloatDeep.IsChecked    = $plan.DebloatDeep
  $cbRemoveOneDrive.IsChecked = $plan.RemoveOneDrive

  $cbTrim.IsChecked           = $plan.SSD_TRIM
  $cbLastAccess.IsChecked     = $plan.SSD_LastAcc
  $cbSched.IsChecked          = $plan.SSD_Sched
  $cbNoHibernate.IsChecked    = $plan.SSD_NoHiber

  $cbMemCompression.IsChecked = $plan.RAM_Compress
  $cbPagefileAuto.IsChecked   = $plan.RAM_PageAuto
  $cbStrictRAM.IsChecked      = $plan.RAM_Strict

  $cbUltimate.IsChecked       = $plan.Adv_Ultimate
  $cbHags.IsChecked           = $plan.Adv_HAGS
  $cbNtfs83.IsChecked         = $plan.Adv_NTFS83
  $cbStartupPrune.IsChecked   = $plan.Adv_Startup
  $cbReTrimSched.IsChecked    = $plan.Adv_ReTrimSch
  $cbDismCleanup.IsChecked    = $plan.Adv_DismClean
  $cbHealthCheck.IsChecked    = $plan.Adv_Health
  $cbThrottleOff.IsChecked    = $plan.Adv_Throttle

  $cbWUdrivers.IsChecked      = $plan.WU_NoDrivers
  $cbOEMclean.IsChecked       = $plan.OEM_Cleanup
  $cbLangClean.IsChecked      = $plan.Lang_Cleanup

  if (-not $PreviewOnly) {
    Write-Log "Aplicando plan inteligente..."
    Run-Selection -DryRun:$false
  }
}

# ===== Reporte =====
function Export-Report {
  try {
    $envp = Get-EnvProfile
    $pkgCsv = $(if (Test-Path $PkgLog) { $PkgLog } else { $null })
    $provCsv = $(if (Test-Path $ProvLog) { $ProvLog } else { $null })

    $rep = [ordered]@{
      RunId       = $RunId
      When        = (Get-Date)
      Computer    = $env:COMPUTERNAME
      User        = $env:USERNAME
      DomainJoin  = $envp.PartOfDomain
      Hardware    = $envp
      PlanSmart   = (Compute-SmartPlan)
      Logs        = $LogFile
      RegBackups  = $RegBackup
      PackagesCSV = $pkgCsv
      ProvCSV     = $provCsv
    }
    $json = ($rep | ConvertTo-Json -Depth 6)
    $json | Set-Content -Path (Join-Path $Base "report_$RunId.json") -Encoding UTF8

    $envStr = $envp | Format-List | Out-String
    $planStr = (Compute-SmartPlan) | Format-List | Out-String

    $html = @"
<html><head><meta charset='utf-8'><title>MAB Report $RunId</title>
<style>body{font-family:Segoe UI,Arial;margin:24px;} code{white-space:pre}</style></head>
<body>
<h2>MAB Optimizer Report</h2>
<p><b>Equipo:</b> $($env:COMPUTERNAME) &nbsp; <b>Usuario:</b> $($env:USERNAME)</p>
<h3>Entorno</h3><pre>$envStr</pre>
<h3>Plan Inteligente</h3><pre>$planStr</pre>
<h3>Archivos</h3>
<ul>
<li>Log: $LogFile</li>
<li>Backups Reg: $RegBackup</li>
<li>Paquetes removidos: $pkgCsv</li>
<li>Provisionados removidos: $provCsv</li>
</ul>
</body></html>
"@
    $html | Set-Content -Path (Join-Path $Base "report_$RunId.html") -Encoding UTF8
    Write-Log "Reportes generados (JSON/HTML) en $Base"
  } catch { Write-Log ("Export-Report error: {0}" -f $_.Exception.Message) }
}

# ===== GUI (WrapPanel + Scroll) =====
$xaml = @"
<Window xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="MAB INGENIERIA DE VALOR - Optimizacion de Equipos (Completo)" Height="780" Width="1180"
        WindowStartupLocation="CenterScreen" ResizeMode="CanResize" Background="#0F172A">
  <Grid Margin="16">
    <Grid.RowDefinitions>
      <RowDefinition Height="Auto"/>
      <RowDefinition Height="Auto"/>
      <RowDefinition Height="*"/>
      <RowDefinition Height="Auto"/>
    </Grid.RowDefinitions>

    <Border CornerRadius="12" Background="#111827" Padding="16">
      <StackPanel>
        <TextBlock Text="MAB INGENIERIA DE VALOR" Foreground="#E5E7EB" FontSize="22" FontWeight="Bold"/>
        <TextBlock Text="Optimizacion de Equipos - Windows 11 Pro (Completo)" Foreground="#9CA3AF" FontSize="14" Margin="0,4,0,0"/>
      </StackPanel>
    </Border>

    <!-- Seleccion de modulos -->
    <ScrollViewer Grid.Row="1" Margin="0,14,0,10"
                  HorizontalScrollBarVisibility="Auto"
                  VerticalScrollBarVisibility="Disabled">
      <WrapPanel Orientation="Horizontal" ItemWidth="Auto" ItemHeight="Auto">

        <!-- Sistema -->
        <GroupBox Header="Sistema" Foreground="#E5E7EB" Background="#111827" BorderBrush="#374151" Padding="10" Margin="0,0,12,0">
          <StackPanel>
            <CheckBox x:Name="cbPrivacy"    Content="Privacidad"  Foreground="#E5E7EB" IsChecked="True" Margin="4"/>
            <CheckBox x:Name="cbPerf"       Content="Rendimiento" Foreground="#E5E7EB" IsChecked="True" Margin="4"/>
            <CheckBox x:Name="cbUX"         Content="UX"          Foreground="#E5E7EB" IsChecked="True" Margin="4"/>
            <CheckBox x:Name="cbUpdate"     Content="Windows Update (prudente)" Foreground="#E5E7EB" IsChecked="True" Margin="4"/>
          </StackPanel>
        </GroupBox>

        <!-- Debloat -->
        <GroupBox Header="Debloat" Foreground="#E5E7EB" Background="#111827" BorderBrush="#374151" Padding="10" Margin="0,0,12,0">
          <StackPanel>
            <RadioButton x:Name="rbNoDebloat"   Content="Sin Debloat" Foreground="#E5E7EB" IsChecked="False" Margin="4"/>
            <RadioButton x:Name="rbDebloatStd"  Content="Debloat estandar" Foreground="#E5E7EB" IsChecked="False" Margin="4"/>
            <RadioButton x:Name="rbDebloatDeep" Content="Debloat profundo - Perfil MAB (Drive + ArcGIS + Office)" Foreground="#E5E7EB" IsChecked="True" Margin="4"/>
            <CheckBox x:Name="cbRemoveOneDrive" Content="Quitar OneDrive (si usan Google Drive)" Foreground="#E5E7EB" IsChecked="True" Margin="4"/>
          </StackPanel>
        </GroupBox>

        <!-- SSD -->
        <GroupBox Header="SSD" Foreground="#E5E7EB" Background="#111827" BorderBrush="#374151" Padding="10" Margin="0,0,12,0">
          <StackPanel>
            <CheckBox x:Name="cbTrim"        Content="Asegurar TRIM + ReTrim ahora" Foreground="#E5E7EB" IsChecked="True" Margin="4"/>
            <CheckBox x:Name="cbLastAccess"  Content="Desactivar 'Ultimo acceso' (menos escrituras)" Foreground="#E5E7EB" IsChecked="True" Margin="4"/>
            <CheckBox x:Name="cbSched"       Content="Habilitar tarea 'Optimizar Unidades'" Foreground="#E5E7EB" IsChecked="True" Margin="4"/>
            <CheckBox x:Name="cbNoHibernate" Content="Quitar hibernacion (liberar espacio)" Foreground="#E5E7EB" IsChecked="False" Margin="4"/>
          </StackPanel>
        </GroupBox>

        <!-- RAM -->
        <GroupBox Header="RAM" Foreground="#E5E7EB" Background="#111827" BorderBrush="#374151" Padding="10" Margin="0,0,12,0">
          <StackPanel>
            <CheckBox x:Name="cbMemCompression" Content="Habilitar Memory Compression" Foreground="#E5E7EB" IsChecked="True" Margin="4"/>
            <CheckBox x:Name="cbPagefileAuto"   Content="Pagefile administrado por el sistema" Foreground="#E5E7EB" IsChecked="True" Margin="4"/>
            <CheckBox x:Name="cbStrictRAM"      Content="Modo RAM estricto (SysMain Off / Indexacion Manual)" Foreground="#E5E7EB" IsChecked="False" Margin="4"/>
          </StackPanel>
        </GroupBox>

        <!-- Avanzado -->
        <GroupBox Header="Avanzado" Foreground="#E5E7EB" Background="#111827" BorderBrush="#374151" Padding="10" Margin="0,0,12,0">
          <StackPanel>
            <CheckBox x:Name="cbUltimate"     Content="Ultimate Performance (solo desktop)" Foreground="#E5E7EB" IsChecked="False" Margin="4"/>
            <CheckBox x:Name="cbHags"         Content="HAGS (GPU Scheduling por hardware)" Foreground="#E5E7EB" IsChecked="False" Margin="4"/>
            <CheckBox x:Name="cbNtfs83"       Content="NTFS 8.3 OFF (volumenes fijos)" Foreground="#E5E7EB" IsChecked="True" Margin="4"/>
            <CheckBox x:Name="cbStartupPrune" Content="Podar Autostart (Run) salvo whitelist" Foreground="#E5E7EB" IsChecked="True" Margin="4"/>
            <CheckBox x:Name="cbReTrimSched"  Content="Programar ReTrim mensual (1er dia, 02:00)" Foreground="#E5E7EB" IsChecked="True" Margin="4"/>
            <CheckBox x:Name="cbDismCleanup"  Content="DISM StartComponentCleanup" Foreground="#E5E7EB" IsChecked="True" Margin="4"/>
            <CheckBox x:Name="cbHealthCheck"  Content="Health Check (DISM + SFC)" Foreground="#E5E7EB" IsChecked="False" Margin="4"/>
            <CheckBox x:Name="cbThrottleOff"  Content="Power Throttling OFF (global)" Foreground="#E5E7EB" IsChecked="True" Margin="4"/>
          </StackPanel>
        </GroupBox>

        <!-- Compatibilidad Win11 -->
        <GroupBox Header="Compatibilidad Win11" Foreground="#E5E7EB" Background="#111827" BorderBrush="#374151" Padding="10" Margin="0,0,12,0">
          <StackPanel>
            <CheckBox x:Name="cbWin11Bypass" Content="Permitir upgrade en HW no soportado (TPM/CPU/SecureBoot)" Foreground="#E5E7EB" IsChecked="False" Margin="4"/>
            <CheckBox x:Name="cbWin11Extras" Content="Incluir extras (Bypass CPU/RAM/Storage)" Foreground="#E5E7EB" IsChecked="False" Margin="4"/>
            <TextBlock Text="Usar solo si la politica de MAB lo permite. Recomendado reiniciar antes de lanzar setup." Foreground="#9CA3AF" FontSize="12" Margin="4,6,4,0"/>
          </StackPanel>
        </GroupBox>

        <!-- Inteligente (Auto) -->
        <GroupBox Header="Inteligente" Foreground="#E5E7EB" Background="#111827" BorderBrush="#374151" Padding="10" Margin="0,0,12,0">
          <StackPanel>
            <Button x:Name="btnSmartPreview" Content="Previsualizar plan (Auto)" Margin="4" Padding="10,6"/>
            <Button x:Name="btnSmartApply"   Content="Aplicar plan (Auto)" Margin="4" Padding="10,6"/>
            <CheckBox x:Name="cbWUdrivers"   Content="Excluir drivers desde Windows Update" Foreground="#E5E7EB" IsChecked="True" Margin="4"/>
            <CheckBox x:Name="cbOEMclean"    Content="Limpieza OEM/trial (segura)" Foreground="#E5E7EB" IsChecked="True" Margin="4"/>
            <CheckBox x:Name="cbLangClean"   Content="Eliminar idiomas no usados (retener es-ES y en-US)" Foreground="#E5E7EB" IsChecked="True" Margin="4"/>
          </StackPanel>
        </GroupBox>

      </WrapPanel>
    </ScrollViewer>

    <GroupBox Grid.Row="2" Header="Registro y resultados" Foreground="#E5E7EB" Background="#111827" BorderBrush="#374151" Padding="10">
      <Grid>
        <TextBox x:Name="txtLog" IsReadOnly="True" TextWrapping="Wrap" VerticalScrollBarVisibility="Auto" Background="#0B1220" Foreground="#E5E7EB"/>
      </Grid>
    </GroupBox>

    <StackPanel Grid.Row="3" Orientation="Horizontal" HorizontalAlignment="Right">
      <Button x:Name="btnDryRun"        Content="Simular (Dry-Run)" Margin="4" Padding="10,6"/>
      <Button x:Name="btnApply"         Content="Aplicar Seleccion" Margin="4" Padding="10,6"/>
      <Button x:Name="btnRevert"        Content="Revertir Cambios" Margin="4" Padding="10,6"/>
      <Button x:Name="btnWin11Apply"    Content="Aplicar Win11 (Bypass On/Off)" Margin="4" Padding="10,6"/>
      <Button x:Name="btnSmartPreview2" Content="Previsualizar Auto" Margin="4" Padding="10,6"/>
      <Button x:Name="btnSmartApply2"   Content="Aplicar Auto" Margin="4" Padding="10,6"/>
      <Button x:Name="btnExit"          Content="Salir" Margin="4" Padding="10,6"/>
      <TextBlock Text="   (c) MAB Ingenieria de Valor" Foreground="#9CA3AF" Margin="8,10,0,0"/>
    </StackPanel>
  </Grid>
</Window>
"@
$window = [Windows.Markup.XamlReader]::Parse($xaml)

# ===== Referencias GUI =====
$global:txtLog    = $window.FindName('txtLog')
$cbPrivacy        = $window.FindName('cbPrivacy')
$cbPerf           = $window.FindName('cbPerf')
$cbUX             = $window.FindName('cbUX')
$cbUpdate         = $window.FindName('cbUpdate')
$rbNoDebloat      = $window.FindName('rbNoDebloat')
$rbDebloatStd     = $window.FindName('rbDebloatStd')
$rbDebloatDeep    = $window.FindName('rbDebloatDeep')
$cbRemoveOneDrive = $window.FindName('cbRemoveOneDrive')
$cbTrim           = $window.FindName('cbTrim')
$cbLastAccess     = $window.FindName('cbLastAccess')
$cbSched          = $window.FindName('cbSched')
$cbNoHibernate    = $window.FindName('cbNoHibernate')
$cbMemCompression = $window.FindName('cbMemCompression')
$cbPagefileAuto   = $window.FindName('cbPagefileAuto')
$cbStrictRAM      = $window.FindName('cbStrictRAM')
$cbUltimate       = $window.FindName('cbUltimate')
$cbHags           = $window.FindName('cbHags')
$cbNtfs83         = $window.FindName('cbNtfs83')
$cbStartupPrune   = $window.FindName('cbStartupPrune')
$cbReTrimSched    = $window.FindName('cbReTrimSched')
$cbDismCleanup    = $window.FindName('cbDismCleanup')
$cbHealthCheck    = $window.FindName('cbHealthCheck')
$cbThrottleOff    = $window.FindName('cbThrottleOff')
$cbWin11Bypass    = $window.FindName('cbWin11Bypass')
$cbWin11Extras    = $window.FindName('cbWin11Extras')
$cbWUdrivers      = $window.FindName('cbWUdrivers')
$cbOEMclean       = $window.FindName('cbOEMclean')
$cbLangClean      = $window.FindName('cbLangClean')
$btnDryRun        = $window.FindName('btnDryRun')
$btnApply         = $window.FindName('btnApply')
$btnRevert        = $window.FindName('btnRevert')
$btnWin11Apply    = $window.FindName('btnWin11Apply')
$btnSmartPreview  = $window.FindName('btnSmartPreview')
$btnSmartApply    = $window.FindName('btnSmartApply')
$btnSmartPreview2 = $window.FindName('btnSmartPreview2')
$btnSmartApply2   = $window.FindName('btnSmartApply2')
$btnExit          = $window.FindName('btnExit')

# ===== Ejecucion principal =====
function Run-Selection {
  param([bool]$DryRun)
  $Script:DryRun = $DryRun
  Start-Transcript -Path $TranscriptFile -Append | Out-Null
  Write-Log ("======== " + ($(if ($DryRun){"DRY-RUN"} else {"EJECUCION"})) + " $((Get-Date).ToString('yyyy-MM-dd HH:mm:ss')) ========")
  if (-not $DryRun) { Create-RestorePoint }

  # Sistema
  if ($cbPrivacy.IsChecked) { Module-Privacy }
  if ($cbPerf.IsChecked)    { Module-Performance }
  if ($cbUX.IsChecked)      { Module-UX }
  if ($cbUpdate.IsChecked)  { Module-Update }
  Harden-TelemetryServices

  # Debloat
  if ($rbDebloatStd.IsChecked)  { Module-DebloatStandard }
  if ($rbDebloatDeep.IsChecked) { Module-DebloatDeepMAB }
  if ($cbRemoveOneDrive.IsChecked) { Remove-OneDrive }

  # SSD
  if ($cbTrim.IsChecked)       { SSD-EnsureTRIM; SSD-ReTrimAll }
  if ($cbLastAccess.IsChecked) { SSD-DisableLastAccess }
  if ($cbSched.IsChecked)      { SSD-EnableOptimizeTask }
  if ($cbNoHibernate.IsChecked){ SSD-RemoveHibernation }

  # RAM
  if ($cbMemCompression.IsChecked) { RAM-EnableMemoryCompression }
  if ($cbPagefileAuto.IsChecked)   { RAM-SetPagefileAuto }
  if ($cbStrictRAM.IsChecked)      { RAM-StrictMode }

  # Avanzado
  if ($cbUltimate.IsChecked)     { Advanced-UltimatePerformance }
  if ($cbHags.IsChecked)         { Advanced-GPUHags }
  if ($cbNtfs83.IsChecked)       { Advanced-NTFS83 }
  if ($cbStartupPrune.IsChecked) { Advanced-StartupPrune }
  if ($cbReTrimSched.IsChecked)  { Advanced-ScheduleReTrim }
  if ($cbDismCleanup.IsChecked)  { Advanced-ComponentCleanup }
  if ($cbHealthCheck.IsChecked)  { Advanced-HealthCheck }
  if ($cbThrottleOff.IsChecked)  { Advanced-PowerThrottlingOff }

  # Corporativo extra
  if ($cbWUdrivers.IsChecked) { Module-WU-ExcludeDrivers }
  if ($cbOEMclean.IsChecked)  { Module-OEMCleanup }
  if ($cbLangClean.IsChecked) { Module-LanguageCleanup }

  Save-StateToFile
  Export-Report
  Stop-Transcript | Out-Null
  Write-Log ("Finalizado. Log: {0}" -f $LogFile)
  Write-Log ("Backups de registro: {0}" -f $RegBackup)
  if (Test-Path $PkgLog)  { Write-Log ("Paquetes removidos (CSV): {0}" -f $PkgLog) }
  if (Test-Path $ProvLog) { Write-Log ("Provisionados removidos (CSV): {0}" -f $ProvLog) }
  Write-Log "Sugerencia: reinicia el equipo para aplicar completamente."
}

# Inicializar estado Win11 en la UI
function Initialize-Win11UI {
  try {
    $st = Get-Win11BypassState
    if ($st) {
      $cbWin11Bypass.IsChecked = $st.Enabled
      $cbWin11Extras.IsChecked = $st.Extras
      Write-Log ("Win11 Bypass estado detectado: " + ($(if($st.Enabled){"ON"}else{"OFF"})) + ($(if($st.Extras){" + extras"}{" "})))
    }
  } catch {}
}
Initialize-Win11UI

# Eventos
$btnDryRun.Add_Click({ Run-Selection -DryRun $true })
$btnApply.Add_Click({
  $res = [System.Windows.MessageBox]::Show("Aplicar los cambios seleccionados?","MAB - Confirmacion",
    [System.Windows.MessageBoxButton]::YesNo,[System.Windows.MessageBoxImage]::Question)
  if ($res -eq "Yes") { Run-Selection -DryRun $false }
})
$btnRevert.Add_Click({
  $res = [System.Windows.MessageBox]::Show("Se importaran backups .reg y se restaurara estado de servicios/hibernacion/pagefile (mejor esfuerzo reinstalar apps). Continuar?",
    "MAB - Reversion",[System.Windows.MessageBoxButton]::YesNo,[System.Windows.MessageBoxImage]::Warning)
  if ($res -eq "Yes") { Start-Transcript -Path $TranscriptFile -Append | Out-Null; Revert-Changes; Stop-Transcript | Out-Null }
})
$btnWin11Apply.Add_Click({
  $target = if ($cbWin11Bypass.IsChecked) { "habilitar" } else { "deshabilitar" }
  $msg = "Esto va a " + $target + " el bypass de requisitos de Windows 11" + $(if($cbWin11Extras.IsChecked){" (incluye extras CPU/RAM/Storage)"}{" "}) + ". Continuar?"
  $res = [System.Windows.MessageBox]::Show($msg,"MAB - Win11 Bypass",[System.Windows.MessageBoxButton]::YesNo,[System.Windows.MessageBoxImage]::Warning)
  if ($res -eq "Yes") { Set-Win11Bypass -Enable ([bool]$cbWin11Bypass.IsChecked) -IncludeExtras ([bool]$cbWin11Extras.IsChecked) }
})
$btnSmartPreview.Add_Click({ Apply-SmartPlan -PreviewOnly })
$btnSmartApply.Add_Click({ Apply-SmartPlan })
$btnSmartPreview2.Add_Click({ Apply-SmartPlan -PreviewOnly })
$btnSmartApply2.Add_Click({ Apply-SmartPlan })
$btnExit.Add_Click({ $window.Close() })

# Cierre ordenado del log al cerrar ventana
$window.add_Closed({ if ($Script:LogWriter){ $Script:LogWriter.Flush(); $Script:LogWriter.Close() } })

Write-Log "MAB INGENIERIA DE VALOR - Optimizador Completo (Windows 11 Pro)."
Write-Log "Usa 'Simular (Dry-Run)' para ver acciones sin aplicarlas."
$window.ShowDialog() | Out-Null
