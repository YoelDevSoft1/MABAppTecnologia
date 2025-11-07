namespace MABAppTecnologia.Models
{
    /// <summary>
    /// Enumeración para los tipos de equipo disponibles
    /// </summary>
    public enum TipoEquipo
    {
        /// <summary>
        /// Equipo propio de MAB - Nomenclatura: SIGLAS-XXXX
        /// </summary>
        Propio,

        /// <summary>
        /// Equipo de alquiler - Nomenclatura: SIGLAS-RUB-XXXX
        /// </summary>
        Alquiler,

        /// <summary>
        /// Equipo para Home Office - Nomenclatura: SIGLAS-HOME-XXXX
        /// </summary>
        HomeOffice
    }

    /// <summary>
    /// Clase para representar un tipo de equipo en el ComboBox
    /// </summary>
    public class TipoEquipoItem
    {
        public TipoEquipo Tipo { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;

        public TipoEquipoItem(TipoEquipo tipo, string nombre, string descripcion)
        {
            Tipo = tipo;
            Nombre = nombre;
            Descripcion = descripcion;
        }

        public override string ToString() => Nombre;
    }
}
