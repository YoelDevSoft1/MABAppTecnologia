using Microsoft.Extensions.Caching.Memory;

namespace MABAppTecnologia.Services
{
    /// <summary>
    /// Implementación del servicio de caching usando MemoryCache
    /// </summary>
    public class CacheService : ICacheService
    {
        private readonly IMemoryCache _memoryCache;
        private readonly ILogService _logService;
        private readonly HashSet<string> _cacheKeys;
        private readonly object _lockObject = new object();

        // Duraciones predeterminadas
        private static readonly TimeSpan DefaultAbsoluteExpiration = TimeSpan.FromHours(1);
        private static readonly TimeSpan DefaultSlidingExpiration = TimeSpan.FromMinutes(20);

        public CacheService(IMemoryCache memoryCache, ILogService logService)
        {
            _memoryCache = memoryCache;
            _logService = logService;
            _cacheKeys = new HashSet<string>();
        }

        public T? Get<T>(string key)
        {
            try
            {
                if (_memoryCache.TryGetValue(key, out T? value))
                {
                    if (_logService is IStructuredLogService structuredLog)
                    {
                        structuredLog.LogDebug("Cache hit: {CacheKey}", key);
                    }
                    else
                    {
                        _logService.LogDebug($"Cache hit: {key}");
                    }
                    return value;
                }

                if (_logService is IStructuredLogService structuredLogMiss)
                {
                    structuredLogMiss.LogDebug("Cache miss: {CacheKey}", key);
                }
                else
                {
                    _logService.LogDebug($"Cache miss: {key}");
                }
                return default;
            }
            catch (Exception ex)
            {
                _logService.LogError($"Error al leer del caché: {key}", ex);
                return default;
            }
        }

        public void Set<T>(string key, T value, TimeSpan? absoluteExpiration = null, TimeSpan? slidingExpiration = null)
        {
            try
            {
                var cacheOptions = new MemoryCacheEntryOptions();

                if (absoluteExpiration.HasValue)
                {
                    cacheOptions.SetAbsoluteExpiration(absoluteExpiration.Value);
                }
                else
                {
                    cacheOptions.SetAbsoluteExpiration(DefaultAbsoluteExpiration);
                }

                if (slidingExpiration.HasValue)
                {
                    cacheOptions.SetSlidingExpiration(slidingExpiration.Value);
                }

                // Registrar callback cuando se elimine el item del caché
                cacheOptions.RegisterPostEvictionCallback((entryKey, entryValue, reason, state) =>
                {
                    lock (_lockObject)
                    {
                        _cacheKeys.Remove(entryKey.ToString() ?? string.Empty);
                    }

                    if (_logService is IStructuredLogService structuredLog)
                    {
                        structuredLog.LogDebug("Cache eviction: {CacheKey}, Reason: {Reason}", entryKey, reason);
                    }
                    else
                    {
                        _logService.LogDebug($"Cache eviction: {entryKey}, Reason: {reason}");
                    }
                });

                _memoryCache.Set(key, value, cacheOptions);

                lock (_lockObject)
                {
                    _cacheKeys.Add(key);
                }

                if (_logService is IStructuredLogService structuredLogSet)
                {
                    structuredLogSet.LogDebug("Cache set: {CacheKey}, TTL: {TTL}min", key,
                        absoluteExpiration?.TotalMinutes ?? DefaultAbsoluteExpiration.TotalMinutes);
                }
                else
                {
                    _logService.LogDebug($"Cache set: {key}");
                }
            }
            catch (Exception ex)
            {
                _logService.LogError($"Error al guardar en caché: {key}", ex);
            }
        }

        public T GetOrCreate<T>(string key, Func<T> factory, TimeSpan? absoluteExpiration = null, TimeSpan? slidingExpiration = null)
        {
            try
            {
                if (_memoryCache.TryGetValue(key, out T? cachedValue))
                {
                    if (_logService is IStructuredLogService structuredLog)
                    {
                        structuredLog.LogDebug("Cache hit (GetOrCreate): {CacheKey}", key);
                    }
                    else
                    {
                        _logService.LogDebug($"Cache hit (GetOrCreate): {key}");
                    }
                    return cachedValue!;
                }

                if (_logService is IStructuredLogService structuredLogMiss)
                {
                    structuredLogMiss.LogDebug("Cache miss (GetOrCreate): {CacheKey}, executing factory", key);
                }
                else
                {
                    _logService.LogDebug($"Cache miss (GetOrCreate): {key}, executing factory");
                }

                var value = factory();
                Set(key, value, absoluteExpiration, slidingExpiration);
                return value;
            }
            catch (Exception ex)
            {
                _logService.LogError($"Error en GetOrCreate: {key}", ex);
                throw;
            }
        }

        public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? absoluteExpiration = null, TimeSpan? slidingExpiration = null)
        {
            try
            {
                if (_memoryCache.TryGetValue(key, out T? cachedValue))
                {
                    if (_logService is IStructuredLogService structuredLog)
                    {
                        structuredLog.LogDebug("Cache hit (GetOrCreateAsync): {CacheKey}", key);
                    }
                    else
                    {
                        _logService.LogDebug($"Cache hit (GetOrCreateAsync): {key}");
                    }
                    return cachedValue!;
                }

                if (_logService is IStructuredLogService structuredLogMiss)
                {
                    structuredLogMiss.LogDebug("Cache miss (GetOrCreateAsync): {CacheKey}, executing factory", key);
                }
                else
                {
                    _logService.LogDebug($"Cache miss (GetOrCreateAsync): {key}, executing factory");
                }

                var value = await factory();
                Set(key, value, absoluteExpiration, slidingExpiration);
                return value;
            }
            catch (Exception ex)
            {
                _logService.LogError($"Error en GetOrCreateAsync: {key}", ex);
                throw;
            }
        }

        public void Remove(string key)
        {
            try
            {
                _memoryCache.Remove(key);
                lock (_lockObject)
                {
                    _cacheKeys.Remove(key);
                }

                if (_logService is IStructuredLogService structuredLog)
                {
                    structuredLog.LogDebug("Cache remove: {CacheKey}", key);
                }
                else
                {
                    _logService.LogDebug($"Cache remove: {key}");
                }
            }
            catch (Exception ex)
            {
                _logService.LogError($"Error al eliminar del caché: {key}", ex);
            }
        }

        public void Clear()
        {
            try
            {
                List<string> keysToRemove;
                lock (_lockObject)
                {
                    keysToRemove = new List<string>(_cacheKeys);
                }

                foreach (var key in keysToRemove)
                {
                    _memoryCache.Remove(key);
                }

                lock (_lockObject)
                {
                    _cacheKeys.Clear();
                }

                _logService.LogInfo($"Cache cleared: {keysToRemove.Count} items removed");
            }
            catch (Exception ex)
            {
                _logService.LogError("Error al limpiar el caché", ex);
            }
        }

        public bool Exists(string key)
        {
            try
            {
                return _memoryCache.TryGetValue(key, out _);
            }
            catch (Exception ex)
            {
                _logService.LogError($"Error al verificar existencia en caché: {key}", ex);
                return false;
            }
        }
    }
}
