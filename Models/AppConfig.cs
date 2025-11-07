namespace MABAppTecnologia.Models
{
    public class AppConfig
    {
        public string MABResourcesPath { get; set; } = @"C:\MAB-Resources";
        public string WallpapersFolder { get; set; } = "Wallpapers";
        public string ProfileImagesFolder { get; set; } = "ProfileImages";
        public string SoftwareFolder { get; set; } = "Software";
        public string LogsFolder { get; set; } = "Logs";
        public string ConfigFolder { get; set; } = "Config";

        // Nombres de archivos de recursos
        public string AdminWallpaper { get; set; } = "admin_wallpaper.jpg";
        public string AdminLockscreen { get; set; } = "admin_lockscreen.jpg";
        public string AdminProfile { get; set; } = "admin_profile.png";
        public string MABWallpaper { get; set; } = "mab_wallpaper.jpg";
        public string MABLockscreen { get; set; } = "mab_lockscreen.jpg";
        public string MABProfile { get; set; } = "mab_profile.png";

        // CSV Configuration
        public string ConsorciosCSVPath { get; set; } = "Config\\consorcios.csv";
    }
}
