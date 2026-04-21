using Microsoft.Extensions.Caching.Memory;

namespace Caching.Caching;

/// <summary>
/// Typed wrapper around IMemoryCache for frequently read, process-local data.
/// Use for: feature flags, app config, reference data, permission lookups.
///
/// Implements the GetOrCreate pattern with stampede protection via
/// locking — prevents multiple threads from flooding the DB on a cold cache.
/// </summary>
public sealed class MemoryCacheService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<MemoryCacheService> _logger;

    // SemaphoreSlim per key prevents cache stampede (dog-pile effect)
    private readonly Dictionary<string, SemaphoreSlim> _locks = new();
    private readonly object _lockLock = new();

    public MemoryCacheService(IMemoryCache cache, ILogger<MemoryCacheService> logger)
    {
        _cache  = cache;
        _logger = logger;
    }

    /// <summary>
    /// Gets a value from memory cache, or calls <paramref name="factory"/>
    /// once (with a per-key lock) to populate it.
    /// </summary>
    public async Task<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        MemoryCacheEntryOptions? options = null,
        CancellationToken ct = default)
    {
        // Fast path — no locking needed on hit
        if (_cache.TryGetValue(key, out T? cached))
        {
            _logger.LogDebug("Memory cache HIT: {Key}", key);
            return cached!;
        }

        // Slow path — acquire per-key lock to prevent stampede
        var semaphore = GetOrCreateSemaphore(key);
        await semaphore.WaitAsync(ct);

        try
        {
            // Double-checked locking — another thread may have populated while we waited
            if (_cache.TryGetValue(key, out cached))
                return cached!;

            _logger.LogDebug("Memory cache MISS: {Key} — calling factory", key);
            var value = await factory(ct);

            _cache.Set(key, value, options ?? CacheEntryOptions.MemoryStandard);
            return value;
        }
        finally
        {
            semaphore.Release();
        }
    }

    public void Invalidate(string key)
    {
        _cache.Remove(key);
        _logger.LogDebug("Memory cache INVALIDATED: {Key}", key);
    }

    public void InvalidateAll(string prefix)
    {
        // IMemoryCache doesn't support prefix scan natively.
        // For prefix invalidation, use a MemoryCache with CancellationToken-based eviction
        // or maintain a key registry manually.
        _logger.LogWarning(
            "Memory cache prefix invalidation requested for '{Prefix}' — " +
            "use Redis for prefix-based eviction in distributed scenarios.", prefix);
    }

    private SemaphoreSlim GetOrCreateSemaphore(string key)
    {
        lock (_lockLock)
        {
            if (!_locks.TryGetValue(key, out var sem))
            {
                sem = new SemaphoreSlim(1, 1);
                _locks[key] = sem;
            }
            return sem;
        }
    }
}
