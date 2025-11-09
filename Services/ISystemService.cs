using MABAppTecnologia.Models;

namespace MABAppTecnologia.Services
{
    public interface ISystemService
    {
        string GetComputerSerialNumber();
        string GetComputerManufacturer();
        string GenerateComputerName(string siglas, string serial, string manufacturer, TipoEquipo tipoEquipo = TipoEquipo.Propio);
        OperationResult RenameComputer(string newName);
        OperationResult CleanDesktopIcons();
        OperationResult CleanTaskbar(string? username = null);
        OperationResult PinAppsToTaskbar(string? username = null);
        OperationResult ApplyPrivacyOptimizations();
        OperationResult ApplyPerformanceOptimizations();
        OperationResult DisableTelemetryServices();
        OperationResult ApplyUXOptimizations();
        OperationResult OptimizeStartup();
        OperationResult RunAdvancedOptimizer();
        OperationResult RemoveBloatwareApps();
        OperationResult CleanTemporaryFiles();
    }
}
