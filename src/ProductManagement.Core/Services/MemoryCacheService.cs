using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using ProductManagement.Core.Interfaces;

namespace ProductManagement.Core.Services;

/// <summary>
/// Thread-safe in-memory caching service implementing ICacheService.
/// - Cached: Cache a generic result up to 5 minutes.
/// - CachedLong: Cache a generic result until cache is removed.
/// Tracks active keys to support pattern/prefix-based invalidation.
/// </summary>
public class MemoryCacheService : ICacheService
{
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<MemoryCacheService> _logger;
    private readonly ConcurrentDictionary<string, bool> _knownKeys = new();

    public MemoryCacheService(IMemoryCache memoryCache, ILogger<MemoryCacheService> logger)
    {
        _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public T? Get<T>(string key)
    {
        if (_memoryCache.TryGetValue(key, out T? value))
        {
            _logger.LogDebug("Cache HIT for key: {Key}", key);
            return value;
        }

        _logger.LogDebug("Cache MISS for key: {Key}", key);
        return default;
    }

    public void Set<T>(string key, T value, TimeSpan? expiration = null)
    {
        var options = new MemoryCacheEntryOptions();

        if (expiration.HasValue)
        {
            options.SetAbsoluteExpiration(expiration.Value);
        }

        options.RegisterPostEvictionCallback((evictedKey, _, _, _) =>
        {
            _knownKeys.TryRemove(evictedKey.ToString() ?? string.Empty, out _);
            _logger.LogDebug("Evicted cache key: {Key}", evictedKey);
        });

        _memoryCache.Set(key, value, options);
        _knownKeys.TryAdd(key, true);
        _logger.LogDebug("Cached key: {Key} (Expires: {Expiration})", key, expiration?.ToString() ?? "Never");
    }

    public void Remove(string key)
    {
        _memoryCache.Remove(key);
        _knownKeys.TryRemove(key, out _);
        _logger.LogDebug("Manually removed cache key: {Key}", key);
    }

    public void RemoveByPrefix(string prefix)
    {
        var matchingKeys = _knownKeys.Keys.Where(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).ToList();
        foreach (var key in matchingKeys)
        {
            Remove(key);
        }
        _logger.LogInformation("Evicted {Count} keys matching prefix '{Prefix}'", matchingKeys.Count, prefix);
    }

    public T GetOrCreate<T>(string key, Func<T> factory, TimeSpan? expiration = null)
    {
        if (_memoryCache.TryGetValue(key, out T? existing))
        {
            _logger.LogDebug("Cache HIT: {Key}", key);
            return existing!;
        }

        _logger.LogDebug("Cache MISS: {Key}. Executing factory...", key);
        var created = factory();
        Set(key, created, expiration);
        return created;
    }

    public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null)
    {
        if (_memoryCache.TryGetValue(key, out T? existing))
        {
            _logger.LogDebug("Cache HIT: {Key}", key);
            return existing!;
        }

        _logger.LogDebug("Cache MISS: {Key}. Executing async factory...", key);
        var created = await factory();
        Set(key, created, expiration);
        return created;
    }

    /// <summary>
    /// Cached - Cache a generic result up to 5 minutes.
    /// </summary>
    public T Cached<T>(string key, Func<T> factory)
    {
        return GetOrCreate(key, factory, TimeSpan.FromMinutes(5));
    }

    /// <summary>
    /// Cached (Async) - Cache a generic result up to 5 minutes.
    /// </summary>
    public async Task<T> CachedAsync<T>(string key, Func<Task<T>> factory)
    {
        return await GetOrCreateAsync(key, factory, TimeSpan.FromMinutes(5));
    }

    /// <summary>
    /// CachedLong - Cache a generic result until cache is removed.
    /// </summary>
    public T CachedLong<T>(string key, Func<T> factory)
    {
        return GetOrCreate(key, factory, expiration: null);
    }

    /// <summary>
    /// CachedLong (Async) - Cache a generic result until cache is removed.
    /// </summary>
    public async Task<T> CachedLongAsync<T>(string key, Func<Task<T>> factory)
    {
        return await GetOrCreateAsync(key, factory, expiration: null);
    }
}
