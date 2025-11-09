namespace MABAppTecnologia.Services
{
    /// <summary>
    /// Servicio de caching para mejorar el rendimiento de la aplicación
    /// </summary>
    public interface ICacheService
    {
        /// <summary>
        /// Obtiene un valor del caché por su clave
        /// </summary>
        T? Get<T>(string key);

        /// <summary>
        /// Guarda un valor en el caché con una duración específica
        /// </summary>
        void Set<T>(string key, T value, TimeSpan? absoluteExpiration = null, TimeSpan? slidingExpiration = null);

        /// <summary>
        /// Obtiene un valor del caché, o lo crea y lo guarda si no existe
        /// </summary>
        T GetOrCreate<T>(string key, Func<T> factory, TimeSpan? absoluteExpiration = null, TimeSpan? slidingExpiration = null);

        /// <summary>
        /// Obtiene un valor del caché de forma asíncrona, o lo crea y lo guarda si no existe
        /// </summary>
        Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? absoluteExpiration = null, TimeSpan? slidingExpiration = null);

        /// <summary>
        /// Elimina un valor del caché
        /// </summary>
        void Remove(string key);

        /// <summary>
        /// Limpia todo el caché
        /// </summary>
        void Clear();

        /// <summary>
        /// Verifica si existe una clave en el caché
        /// </summary>
        bool Exists(string key);
    }
}
