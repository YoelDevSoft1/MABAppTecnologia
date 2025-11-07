namespace MABAppTecnologia.Models
{
    public class SoftwareItem
    {
        public string Name { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string FullPath { get; set; } = string.Empty;
        public string Category { get; set; } = "General"; // Categoría basada en subcarpeta
        public bool IsSelected { get; set; }
        public bool IsInstalling { get; set; }
        public bool IsInstalled { get; set; }
        public string Status { get; set; } = "Pendiente";
    }
}
