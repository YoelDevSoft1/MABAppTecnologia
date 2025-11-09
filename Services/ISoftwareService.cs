using MABAppTecnologia.Models;

namespace MABAppTecnologia.Services
{
    public interface ISoftwareService
    {
        List<SoftwareItem> GetAvailableSoftware();
        Task<OperationResult> InstallSoftware(SoftwareItem software, IProgress<string>? progress = null);
        Task<List<OperationResult>> InstallSelectedSoftware(List<SoftwareItem> softwareList, IProgress<string>? progress = null);
    }
}
