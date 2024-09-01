namespace HomeSensors.Model.Cache;

public enum CacheExpirationMode
{
    /// <summary>
    /// Sliding expiration. The item only expires if not requested within the cache duration. If requested during cache duration, the duration timer restarts.
    /// Expired item will remain in cache until refreshed on the next request.
    /// </summary>
    Sliding,

    /// <summary>
    /// Absolute expiration. The item expires at the end of the duration starting from when it was cached.
    /// Expired item will remain in cache until refreshed on the next request.
    /// </summary>
    Absolute,

    /// <summary>
    /// Absolute expiration with immediate eviction. The item expires at the end of the duration starting from when it was cached.
    /// Expired item is immediately removed from memory.
    /// </summary>
    ImmediateAbsolute
}
