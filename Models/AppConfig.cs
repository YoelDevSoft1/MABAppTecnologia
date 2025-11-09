using System.ComponentModel.DataAnnotations;

namespace MABAppTecnologia.Models
{
    /// <summary>
    /// Configuración de la aplicación con validación mediante DataAnnotations
    /// </summary>
    public class AppConfig
    {
        /// <summary>
        /// Ruta raíz donde se almacenan los recursos de MAB
        /// </summary>
        [Required(ErrorMessage = "La ruta de recursos MAB es obligatoria")]
        [MinLength(3, ErrorMessage = "La ruta debe tener al menos 3 caracteres")]
        public string MABResourcesPath { get; set; } = @"C:\MAB-Resources";

        /// <summary>
        /// Carpeta de wallpapers dentro de MABResourcesPath
        /// </summary>
        [Required(ErrorMessage = "La carpeta de wallpapers es obligatoria")]
        [MinLength(1, ErrorMessage = "El nombre de carpeta no puede estar vacío")]
        public string WallpapersFolder { get; set; } = "Wallpapers";

        /// <summary>
        /// Carpeta de imágenes de perfil dentro de MABResourcesPath
        /// </summary>
        [Required(ErrorMessage = "La carpeta de imágenes de perfil es obligatoria")]
        [MinLength(1, ErrorMessage = "El nombre de carpeta no puede estar vacío")]
        public string ProfileImagesFolder { get; set; } = "ProfileImages";

        /// <summary>
        /// Carpeta de software dentro de MABResourcesPath
        /// </summary>
        [Required(ErrorMessage = "La carpeta de software es obligatoria")]
        [MinLength(1, ErrorMessage = "El nombre de carpeta no puede estar vacío")]
        public string SoftwareFolder { get; set; } = "Software";

        /// <summary>
        /// Carpeta de logs dentro de MABResourcesPath
        /// </summary>
        [Required(ErrorMessage = "La carpeta de logs es obligatoria")]
        [MinLength(1, ErrorMessage = "El nombre de carpeta no puede estar vacío")]
        public string LogsFolder { get; set; } = "Logs";

        /// <summary>
        /// Carpeta de configuración
        /// </summary>
        [Required(ErrorMessage = "La carpeta de configuración es obligatoria")]
        [MinLength(1, ErrorMessage = "El nombre de carpeta no puede estar vacío")]
        public string ConfigFolder { get; set; } = "Config";

        // ===== Nombres de archivos de recursos =====

        /// <summary>
        /// Nombre del archivo de wallpaper para usuario ADMIN
        /// </summary>
        [Required(ErrorMessage = "El nombre del wallpaper de ADMIN es obligatorio")]
        [RegularExpression(@"^[\w\-. ]+\.(jpg|jpeg|png|bmp)$", ErrorMessage = "Debe ser un nombre de archivo de imagen válido (.jpg, .png, .bmp)")]
        public string AdminWallpaper { get; set; } = "admin_wallpaper.jpg";

        /// <summary>
        /// Nombre del archivo de lockscreen para usuario ADMIN
        /// </summary>
        [Required(ErrorMessage = "El nombre del lockscreen de ADMIN es obligatorio")]
        [RegularExpression(@"^[\w\-. ]+\.(jpg|jpeg|png|bmp)$", ErrorMessage = "Debe ser un nombre de archivo de imagen válido (.jpg, .png, .bmp)")]
        public string AdminLockscreen { get; set; } = "admin_lockscreen.jpg";

        /// <summary>
        /// Nombre del archivo de perfil para usuario ADMIN
        /// </summary>
        [Required(ErrorMessage = "El nombre de la imagen de perfil de ADMIN es obligatorio")]
        [RegularExpression(@"^[\w\-. ]+\.(jpg|jpeg|png|bmp)$", ErrorMessage = "Debe ser un nombre de archivo de imagen válido (.jpg, .png, .bmp)")]
        public string AdminProfile { get; set; } = "admin_profile.png";

        /// <summary>
        /// Nombre del archivo de wallpaper para usuario MAB
        /// </summary>
        [Required(ErrorMessage = "El nombre del wallpaper de MAB es obligatorio")]
        [RegularExpression(@"^[\w\-. ]+\.(jpg|jpeg|png|bmp)$", ErrorMessage = "Debe ser un nombre de archivo de imagen válido (.jpg, .png, .bmp)")]
        public string MABWallpaper { get; set; } = "mab_wallpaper.jpg";

        /// <summary>
        /// Nombre del archivo de lockscreen para usuario MAB
        /// </summary>
        [Required(ErrorMessage = "El nombre del lockscreen de MAB es obligatorio")]
        [RegularExpression(@"^[\w\-. ]+\.(jpg|jpeg|png|bmp)$", ErrorMessage = "Debe ser un nombre de archivo de imagen válido (.jpg, .png, .bmp)")]
        public string MABLockscreen { get; set; } = "mab_lockscreen.jpg";

        /// <summary>
        /// Nombre del archivo de perfil para usuario MAB
        /// </summary>
        [Required(ErrorMessage = "El nombre de la imagen de perfil de MAB es obligatorio")]
        [RegularExpression(@"^[\w\-. ]+\.(jpg|jpeg|png|bmp)$", ErrorMessage = "Debe ser un nombre de archivo de imagen válido (.jpg, .png, .bmp)")]
        public string MABProfile { get; set; } = "mab_profile.png";

        // ===== CSV Configuration =====

        /// <summary>
        /// Ruta relativa al archivo CSV de consorcios
        /// </summary>
        [Required(ErrorMessage = "La ruta del CSV de consorcios es obligatoria")]
        [RegularExpression(@"^[\w\\\-. ]+\.csv$", ErrorMessage = "Debe ser un archivo .csv válido")]
        public string ConsorciosCSVPath { get; set; } = "Config\\consorcios.csv";
    }
}
