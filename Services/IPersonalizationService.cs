using MABAppTecnologia.Models;

namespace MABAppTecnologia.Services
{
    public interface IPersonalizationService
    {
        OperationResult SetWallpaperForUser(string username, string wallpaperFileName);
        OperationResult SetLockScreenForUser(string username, string lockscreenFileName);
        OperationResult SetUserProfileImage(string username, string profileImageFileName);
        OperationResult ApplyAllPersonalization(string username, bool isAdmin);
    }
}
