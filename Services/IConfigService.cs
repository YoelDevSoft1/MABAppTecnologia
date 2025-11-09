using MABAppTecnologia.Models;

namespace MABAppTecnologia.Services
{
    public interface IConfigService
    {
        AppConfig AppConfig { get; }
        List<ConsorcioConfig> LoadConsorcios();
        void EnsureResourcesInSystemFolder();
    }
}
