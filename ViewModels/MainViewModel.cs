using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using MABAppTecnologia.Helpers;
using MABAppTecnologia.Models;
using MABAppTecnologia.Services;

namespace MABAppTecnologia.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly LogService _logService;
        private readonly ConfigService _configService;
        private readonly SystemService _systemService;
        private readonly UserService _userService;
        private readonly PersonalizationService _personalizationService;
        private readonly SoftwareService _softwareService;

        private int _currentStep;
        private string _statusMessage;
        private bool _isProcessing;
        private int _progressValue;
        private string _computerSerial;
        private string _computerManufacturer;
        private string _generatedComputerName;
        private ConsorcioConfig? _selectedConsorcio;
        private TipoEquipoItem? _selectedTipoEquipo;
        
        // Optimization options
        private bool _enableAdvancedOptimizer = true;
        private bool _enablePrivacyOptimizations = true;
        private bool _enablePerformanceOptimizations = true;
        private bool _disableTelemetryServices = true;
        private bool _enableUXOptimizations = true;
        private bool _removeBloatware = false;
        private bool _optimizeStartup = true;
        private bool _cleanTemporaryFiles = true;

        public MainViewModel()
        {
            _logService = new LogService();
            _configService = new ConfigService(_logService);
            _systemService = new SystemService(_logService);
            _userService = new UserService(_logService);
            _personalizationService = new PersonalizationService(_logService, _configService);
            _softwareService = new SoftwareService(_logService);

            _statusMessage = "Bienvenido a MAB APP TECNOLOGIA";
            _currentStep = 1;
            _computerSerial = string.Empty;
            _computerManufacturer = string.Empty;
            _generatedComputerName = string.Empty;

            Consorcios = new ObservableCollection<ConsorcioConfig>();
            SoftwareList = new ObservableCollection<SoftwareItem>();
            TiposEquipo = new ObservableCollection<TipoEquipoItem>();

            // Inicializar tipos de equipo
            TiposEquipo.Add(new TipoEquipoItem(TipoEquipo.Propio, "Equipo Propio", "Nomenclatura: SIGLAS-XXXX"));
            TiposEquipo.Add(new TipoEquipoItem(TipoEquipo.Alquiler, "Equipo de Alquiler", "Nomenclatura: SIGLAS-RUB-XXXX"));
            TiposEquipo.Add(new TipoEquipoItem(TipoEquipo.HomeOffice, "Equipo Home Office", "Nomenclatura: SIGLAS-HOME-XXXX"));
            
            // Seleccionar "Equipo Propio" por defecto
            SelectedTipoEquipo = TiposEquipo[0];

            // Commands
            LoadDataCommand = new AsyncRelayCommand(async _ => await LoadInitialData());
            NextStepCommand = new RelayCommand(_ => NextStep(), _ => !IsProcessing);
            PreviousStepCommand = new RelayCommand(_ => PreviousStep(), _ => !IsProcessing && CurrentStep > 1);
            ExecuteAllCommand = new AsyncRelayCommand(async _ => await ExecuteAllSteps(), _ => !IsProcessing);
            ExecuteCurrentStepCommand = new AsyncRelayCommand(async _ => await ExecuteCurrentStep(), _ => !IsProcessing);

            // Auto-load data on initialization
            _ = LoadInitialData();
        }

        // Properties
        public int CurrentStep
        {
            get => _currentStep;
            set => SetProperty(ref _currentStep, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public bool IsProcessing
        {
            get => _isProcessing;
            set => SetProperty(ref _isProcessing, value);
        }

        public int ProgressValue
        {
            get => _progressValue;
            set => SetProperty(ref _progressValue, value);
        }

        public string ComputerSerial
        {
            get => _computerSerial;
            set => SetProperty(ref _computerSerial, value);
        }

        public string ComputerManufacturer
        {
            get => _computerManufacturer;
            set => SetProperty(ref _computerManufacturer, value);
        }

        public string GeneratedComputerName
        {
            get => _generatedComputerName;
            set => SetProperty(ref _generatedComputerName, value);
        }

        public ConsorcioConfig? SelectedConsorcio
        {
            get => _selectedConsorcio;
            set
            {
                SetProperty(ref _selectedConsorcio, value);
                if (value != null && !string.IsNullOrEmpty(ComputerSerial))
                {
                    GenerateComputerName();
                }
            }
        }

        public TipoEquipoItem? SelectedTipoEquipo
        {
            get => _selectedTipoEquipo;
            set
            {
                SetProperty(ref _selectedTipoEquipo, value);
                if (value != null && SelectedConsorcio != null && !string.IsNullOrEmpty(ComputerSerial))
                {
                    GenerateComputerName();
                }
            }
        }

        public ObservableCollection<ConsorcioConfig> Consorcios { get; }
        public ObservableCollection<SoftwareItem> SoftwareList { get; }
        public ObservableCollection<TipoEquipoItem> TiposEquipo { get; }
        
        // Optimization Properties
        public bool EnableAdvancedOptimizer
        {
            get => _enableAdvancedOptimizer;
            set => SetProperty(ref _enableAdvancedOptimizer, value);
        }
        
        public bool EnablePrivacyOptimizations
        {
            get => _enablePrivacyOptimizations;
            set => SetProperty(ref _enablePrivacyOptimizations, value);
        }
        
        public bool EnablePerformanceOptimizations
        {
            get => _enablePerformanceOptimizations;
            set => SetProperty(ref _enablePerformanceOptimizations, value);
        }
        
        public bool DisableTelemetryServices
        {
            get => _disableTelemetryServices;
            set => SetProperty(ref _disableTelemetryServices, value);
        }
        
        public bool EnableUXOptimizations
        {
            get => _enableUXOptimizations;
            set => SetProperty(ref _enableUXOptimizations, value);
        }
        
        public bool RemoveBloatware
        {
            get => _removeBloatware;
            set => SetProperty(ref _removeBloatware, value);
        }
        
        public bool OptimizeStartup
        {
            get => _optimizeStartup;
            set => SetProperty(ref _optimizeStartup, value);
        }
        
        public bool CleanTemporaryFiles
        {
            get => _cleanTemporaryFiles;
            set => SetProperty(ref _cleanTemporaryFiles, value);
        }

        // Commands
        public ICommand LoadDataCommand { get; }
        public ICommand NextStepCommand { get; }
        public ICommand PreviousStepCommand { get; }
        public ICommand ExecuteAllCommand { get; }
        public ICommand ExecuteCurrentStepCommand { get; }

        // Methods
        private async Task LoadInitialData()
        {
            try
            {
                IsProcessing = true;
                StatusMessage = "Cargando configuración...";

                // Cargar consorcios
                var consorcios = _configService.LoadConsorcios();
                Consorcios.Clear();
                foreach (var consorcio in consorcios)
                {
                    Consorcios.Add(consorcio);
                }

                if (Consorcios.Count == 0)
                {
                    StatusMessage = "⚠ ADVERTENCIA: No se cargaron consorcios. Verificar Config/consorcios.csv";
                    _logService.LogWarning("No se cargaron consorcios desde el CSV");
                    MessageBox.Show(
                        "No se encontraron consorcios en el archivo Config/consorcios.csv\n\n" +
                        "Por favor, verifica que el archivo existe y tiene el formato correcto:\n" +
                        "Consorcio,Siglas,ContraseñaAdmin,PinAdmin",
                        "Advertencia",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning
                    );
                }

                // Obtener información del equipo
                ComputerSerial = _systemService.GetComputerSerialNumber();
                ComputerManufacturer = _systemService.GetComputerManufacturer();

                // Cargar software disponible
                var software = _softwareService.GetAvailableSoftware();
                SoftwareList.Clear();
                foreach (var item in software)
                {
                    SoftwareList.Add(item);
                }

                // Asegurar que los recursos estén en C:\MAB-Resources
                await Task.Run(() => _configService.EnsureResourcesInSystemFolder());

                if (Consorcios.Count > 0)
                {
                    StatusMessage = $"✓ Configuración cargada: {Consorcios.Count} consorcios, {SoftwareList.Count} aplicaciones";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"✗ Error al cargar configuración: {ex.Message}";
                _logService.LogError("Error en LoadInitialData", ex);
                MessageBox.Show(
                    $"Error al cargar configuración:\n\n{ex.Message}\n\nRevisa el archivo de log para más detalles.",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
            finally
            {
                IsProcessing = false;
            }
        }

        private void GenerateComputerName()
        {
            if (SelectedConsorcio != null && !string.IsNullOrEmpty(ComputerSerial) && SelectedTipoEquipo != null)
            {
                GeneratedComputerName = _systemService.GenerateComputerName(
                    SelectedConsorcio.Siglas,
                    ComputerSerial,
                    ComputerManufacturer,
                    SelectedTipoEquipo.Tipo
                );
            }
        }

        private void NextStep()
        {
            if (CurrentStep < 5)
            {
                CurrentStep++;
                StatusMessage = GetStepMessage(CurrentStep);
            }
        }

        private void PreviousStep()
        {
            if (CurrentStep > 1)
            {
                CurrentStep--;
                StatusMessage = GetStepMessage(CurrentStep);
            }
        }

        private string GetStepMessage(int step)
        {
            return step switch
            {
                1 => "Paso 1: Configuración de nomenclatura del equipo",
                2 => "Paso 2: Gestión de usuarios",
                3 => "Paso 3: Personalización del sistema",
                4 => "Paso 4: Instalación de software",
                5 => "Paso 5: Optimización del sistema",
                _ => "MAB APP TECNOLOGIA"
            };
        }

        private async Task ExecuteCurrentStep()
        {
            try
            {
                IsProcessing = true;
                ProgressValue = 0;

                switch (CurrentStep)
                {
                    case 1:
                        await ExecuteStep1_Nomenclatura();
                        break;
                    case 2:
                        await ExecuteStep2_Usuarios();
                        break;
                    case 3:
                        await ExecuteStep3_Personalizacion();
                        break;
                    case 4:
                        await ExecuteStep4_Software();
                        break;
                    case 5:
                        await ExecuteStep5_Optimizacion();
                        break;
                }

                ProgressValue = 100;
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
                _logService.LogError($"Error en paso {CurrentStep}", ex);
                MessageBox.Show($"Error en el paso actual: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsProcessing = false;
            }
        }

        private async Task ExecuteAllSteps()
        {
            try
            {
                IsProcessing = true;
                ProgressValue = 0;
                StatusMessage = "Ejecutando todos los pasos...";

                var totalSteps = 5;
                var currentProgress = 0;

                for (int step = 1; step <= totalSteps; step++)
                {
                    CurrentStep = step;
                    await ExecuteCurrentStep();
                    currentProgress = (step * 100) / totalSteps;
                    ProgressValue = currentProgress;
                }

                StatusMessage = "¡Configuración completa! El equipo está listo.";
                MessageBox.Show(
                    "Configuración completada exitosamente.\n\nSe recomienda reiniciar el equipo para aplicar todos los cambios.",
                    "Éxito",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );

                _logService.LogSuccess("Configuración completa exitosa");
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error en proceso completo: {ex.Message}";
                _logService.LogError("Error en ExecuteAllSteps", ex);
                MessageBox.Show($"Error durante el proceso: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsProcessing = false;
            }
        }

        private async Task ExecuteStep1_Nomenclatura()
        {
            await Task.Run(() =>
            {
                if (SelectedConsorcio == null)
                {
                    StatusMessage = "Error: Debe seleccionar un consorcio";
                    return;
                }

                StatusMessage = $"Renombrando equipo a {GeneratedComputerName}...";
                var result = _systemService.RenameComputer(GeneratedComputerName);

                StatusMessage = result.Success
                    ? $"✓ Equipo renombrado a {GeneratedComputerName}"
                    : $"✗ Error al renombrar: {result.Message}";
            });
        }

        private async Task ExecuteStep2_Usuarios()
        {
            await Task.Run(() =>
            {
                if (SelectedConsorcio == null)
                {
                    StatusMessage = "Error: Debe seleccionar un consorcio";
                    return;
                }

                StatusMessage = "Configurando usuario ADMIN...";
                var adminResult = _userService.ConfigureAdminUser(
                    SelectedConsorcio.ContraseñaAdmin,
                    SelectedConsorcio.PinAdmin
                );

                if (!adminResult.Success)
                {
                    StatusMessage = $"✗ Error configurando ADMIN: {adminResult.Message}";
                    return;
                }

                StatusMessage = "Creando usuario MAB...";
                var mabResult = _userService.CreateMABUser();

                StatusMessage = mabResult.Success
                    ? "✓ Usuarios configurados correctamente"
                    : $"✗ Error creando usuario MAB: {mabResult.Message}";
            });
        }

        private async Task ExecuteStep3_Personalizacion()
        {
            await Task.Run(() =>
            {
                var results = new List<(string User, bool Success, string Message)>();
                var currentUser = Environment.UserName;

                // Intentar personalizar ADMIN (si es necesario)
                StatusMessage = "Aplicando personalización para ADMIN...";
                var adminResult = _personalizationService.ApplyAllPersonalization("ADMIN", true);
                results.Add(("ADMIN", adminResult.Success, adminResult.Message));

                if (!adminResult.Success)
                {
                    _logService.LogWarning($"Error al personalizar ADMIN: {adminResult.Message}");
                    // Continuar con MAB aunque ADMIN haya fallado
                }

                // Intentar personalizar MAB (si es necesario)
                StatusMessage = "Aplicando personalización para MAB...";
                var mabResult = _personalizationService.ApplyAllPersonalization("MAB", false);
                results.Add(("MAB", mabResult.Success, mabResult.Message));

                if (!mabResult.Success)
                {
                    _logService.LogWarning($"Error al personalizar MAB: {mabResult.Message}");
                }

                // Determinar mensaje final basado en los resultados
                var successful = results.Where(r => r.Success).ToList();
                var failed = results.Where(r => !r.Success).ToList();

                if (successful.Count == results.Count)
                {
                    StatusMessage = "✓ Personalización aplicada correctamente para todos los usuarios";
                }
                else if (successful.Count > 0)
                {
                    var successUsers = string.Join(", ", successful.Select(r => r.User));
                    var failedUsers = string.Join(", ", failed.Select(r => r.User));
                    StatusMessage = $"⚠ Personalización aplicada para {successUsers}. Falló para: {failedUsers}";
                    
                    // Si el usuario actual está en los exitosos, considerar éxito parcial
                    if (successful.Any(r => r.User.Equals(currentUser, StringComparison.OrdinalIgnoreCase)))
                    {
                        _logService.LogInfo($"Personalización exitosa para usuario actual ({currentUser})");
                    }
                }
                else
                {
                    var errors = string.Join("; ", failed.Select(r => $"{r.User}: {r.Message}"));
                    StatusMessage = $"✗ Error: Algunas operaciones de personalización fallaron. {errors}";
                }
            });
        }

        private async Task ExecuteStep4_Software()
        {
            var selectedSoftware = SoftwareList.Where(s => s.IsSelected).ToList();

            if (!selectedSoftware.Any())
            {
                StatusMessage = "No hay software seleccionado para instalar";
                return;
            }

            StatusMessage = $"Instalando {selectedSoftware.Count} aplicaciones...";

            var progress = new Progress<string>(msg =>
            {
                StatusMessage = msg;
            });

            await _softwareService.InstallSelectedSoftware(selectedSoftware, progress);

            var successCount = selectedSoftware.Count(s => s.IsInstalled);
            StatusMessage = $"✓ Instalación completada: {successCount}/{selectedSoftware.Count} exitosas";
        }

        private async Task ExecuteStep5_Optimizacion()
        {
            await Task.Run(() =>
            {
                int totalOperations = 0;
                int successfulOperations = 0;
                
                // Count selected operations
                if (EnablePrivacyOptimizations) totalOperations++;
                if (EnablePerformanceOptimizations) totalOperations++;
                if (DisableTelemetryServices) totalOperations++;
                if (EnableUXOptimizations) totalOperations++;
                if (RemoveBloatware) totalOperations++;
                if (OptimizeStartup) totalOperations++;
                if (CleanTemporaryFiles) totalOperations++;
                totalOperations += 3; // Desktop cleanup + Taskbar cleanup (ADMIN/MAB) with icons (always)
                
                StatusMessage = "Iniciando optimizaciones del sistema...";
                _logService.LogInfo($"Ejecutando {totalOperations} operaciones de optimización");
                
                // Basic optimizations (always)
                StatusMessage = "Limpiando iconos del escritorio...";
                var desktopResult = _systemService.CleanDesktopIcons();
                if (desktopResult.Success) successfulOperations++;

                StatusMessage = "Configurando barra de tareas para ADMIN...";
                var taskbarAdminResult = _systemService.CleanTaskbar("ADMIN");
                if (taskbarAdminResult.Success) successfulOperations++;

                StatusMessage = "Configurando barra de tareas para MAB...";
                var taskbarMabResult = _systemService.CleanTaskbar("MAB");
                if (taskbarMabResult.Success) successfulOperations++;

                // Privacy optimizations
                if (EnablePrivacyOptimizations)
                {
                    StatusMessage = "Aplicando optimizaciones de privacidad...";
                    var privacyResult = _systemService.ApplyPrivacyOptimizations();
                    if (privacyResult.Success) successfulOperations++;
                    _logService.LogInfo($"Privacidad: {privacyResult.Message}");
                }

                // Performance optimizations
                if (EnablePerformanceOptimizations)
                {
                    StatusMessage = "Aplicando optimizaciones de rendimiento...";
                    var perfResult = _systemService.ApplyPerformanceOptimizations();
                    if (perfResult.Success) successfulOperations++;
                    _logService.LogInfo($"Rendimiento: {perfResult.Message}");
                }

                // Disable telemetry services
                if (DisableTelemetryServices)
                {
                    StatusMessage = "Deshabilitando servicios de telemetría...";
                    var telemetryResult = _systemService.DisableTelemetryServices();
                    if (telemetryResult.Success) successfulOperations++;
                    _logService.LogInfo($"Telemetría: {telemetryResult.Message}");
                }

                // UX optimizations
                if (EnableUXOptimizations)
                {
                    StatusMessage = "Aplicando optimizaciones de experiencia de usuario...";
                    var uxResult = _systemService.ApplyUXOptimizations();
                    if (uxResult.Success) successfulOperations++;
                    _logService.LogInfo($"UX: {uxResult.Message}");
                }

                // Remove bloatware
                if (RemoveBloatware)
                {
                    StatusMessage = "Eliminando aplicaciones no deseadas...";
                    var bloatResult = _systemService.RemoveBloatwareApps();
                    if (bloatResult.Success) successfulOperations++;
                    _logService.LogInfo($"Bloatware: {bloatResult.Message}");
                }

                // Optimize startup
                if (OptimizeStartup)
                {
                    StatusMessage = "Optimizando inicio del sistema...";
                    var startupResult = _systemService.OptimizeStartup();
                    if (startupResult.Success) successfulOperations++;
                    _logService.LogInfo($"Inicio: {startupResult.Message}");
                }

                // Clean temporary files
                if (CleanTemporaryFiles)
                {
                    StatusMessage = "Limpiando archivos temporales...";
                    var tempResult = _systemService.CleanTemporaryFiles();
                    if (tempResult.Success) successfulOperations++;
                    _logService.LogInfo($"Archivos temp: {tempResult.Message}");
                }

                // Advanced PowerShell optimizer (if enabled)
                if (EnableAdvancedOptimizer)
                {
                    StatusMessage = "Ejecutando optimizador avanzado (PowerShell)...";
                    _logService.LogInfo("Iniciando OptimizerMAB.ps1");
                    
                    var advancedResult = _systemService.RunAdvancedOptimizer();
                    
                    if (advancedResult.Success)
                    {
                        StatusMessage = "✓ Optimizador avanzado completado";
                        _logService.LogSuccess($"OptimizerMAB.ps1: {advancedResult.Message}");
                    }
                    else
                    {
                        StatusMessage = $"⚠ Optimizador avanzado: {advancedResult.Message}";
                        _logService.LogWarning($"OptimizerMAB.ps1 falló: {advancedResult.Message}");
                    }
                }

                // Final status
                var successRate = (successfulOperations * 100) / totalOperations;
                StatusMessage = successRate == 100
                    ? $"✓ Optimización completada: {successfulOperations}/{totalOperations} exitosas"
                    : $"⚠ Optimización parcial: {successfulOperations}/{totalOperations} exitosas";
                
                _logService.LogSuccess($"Optimización completada: {successfulOperations}/{totalOperations} operaciones exitosas");
            });
        }
    }
}
