using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;

namespace Caching.Caching;

/// <summary>
/// Predefined TTL profiles for different data volatility tiers.
/// Use the right profile per data type — avoid arbitrary magic numbers.
/// </summary>
public static class CacheEntryOptions
{
    // ── Distributed Cache (Redis) ─────────────────────────────────────────────

    /// <summary>Rarely changing data — feature flags, config, categories.</summary>
    public static DistributedCacheEntryOptions Long => new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(6),
        SlidingExpiration               = TimeSpan.FromHours(1)
    };

    /// <summary>Moderately volatile — product listings, user profiles.</summary>
    public static DistributedCacheEntryOptions Standard => new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30),
        SlidingExpiration               = TimeSpan.FromMinutes(10)
    };

    /// <summary>Frequently changing — inventory counts, pricing.</summary>
    public static DistributedCacheEntryOptions Short => new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
    };

    /// <summary>Per-request deduplication window — search results, pagination.</summary>
    public static DistributedCacheEntryOptions Brief => new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30)
    };

    // ── In-Memory Cache ───────────────────────────────────────────────────────

    public static MemoryCacheEntryOptions MemoryLong => new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(60),
        SlidingExpiration               = TimeSpan.FromMinutes(15),
        Priority                        = CacheItemPriority.Normal,
        Size                            = 1
    };

    public static MemoryCacheEntryOptions MemoryStandard => new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10),
        SlidingExpiration               = TimeSpan.FromMinutes(3),
        Priority                        = CacheItemPriority.Normal,
        Size                            = 1
    };

    public static MemoryCacheEntryOptions MemoryShort => new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2),
        Priority                        = CacheItemPriority.Low,
        Size                            = 1
    };
}