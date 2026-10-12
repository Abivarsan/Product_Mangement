namespace ProductManagement.Core.Interfaces;

/// <summary>
/// Cache service interface defining retrieval, storage, and caching policies.
/// - Cached: Cache a generic result up to 5 minutes.
/// - CachedLong: Cache a generic result until cache is removed.
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Retrieves an item from cache if present.
    /// </summary>
    T? Get<T>(string key);

    /// <summary>
    /// Explicitly sets an item in cache with optional expiration.
    /// </summary>
    void Set<T>(string key, T value, TimeSpan? expiration = null);

    /// <summary>
    /// Removes a specific item by key.
    /// </summary>
    void Remove(string key);

    /// <summary>
    /// Removes all keys matching a given prefix.
    /// </summary>
    void RemoveByPrefix(string prefix);

    /// <summary>
    /// Standard generic GetOrCreate pattern with custom or default expiration.
    /// </summary>
    T GetOrCreate<T>(string key, Func<T> factory, TimeSpan? expiration = null);
    Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null);

    /// <summary>
    /// Cached - Cache a generic result up to 5 minutes.
    /// </summary>
    T Cached<T>(string key, Func<T> factory);
    Task<T> CachedAsync<T>(string key, Func<Task<T>> factory);

    /// <summary>
    /// CachedLong - Cache a generic result until cache is removed.
    /// </summary>
    T CachedLong<T>(string key, Func<T> factory);
    Task<T> CachedLongAsync<T>(string key, Func<Task<T>> factory);
}
