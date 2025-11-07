using CsvHelper.Configuration.Attributes;

namespace MABAppTecnologia.Models
{
    public class ConsorcioConfig
    {
        [Name("Consorcio")]
        public string Consorcio { get; set; } = string.Empty;

        [Name("Siglas")]
        public string Siglas { get; set; } = string.Empty;

        [Name("ContraseñaAdmin")]
        public string ContraseñaAdmin { get; set; } = string.Empty;

        [Name("PinAdmin")]
        public string PinAdmin { get; set; } = string.Empty;
    }
}
