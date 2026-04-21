using Caching.Caching;
using StackExchange.Redis;

namespace Caching.Caching;

/// <summary>
/// Uses Redis SCAN + DEL to invalidate all keys matching a prefix pattern.
/// Required when list keys are dynamic (unknown page/filter combinations).
/// Register as scoped or transient — IConnectionMultiplexer is singleton.
/// </summary>
public sealed class RedisInvalidationService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisInvalidationService> _logger;

    public RedisInvalidationService(
        IConnectionMultiplexer redis,
        ILogger<RedisInvalidationService> logger)
    {
        _redis  = redis;
        _logger = logger;
    }

    /// <summary>
    /// Deletes all Redis keys matching the given prefix pattern.
    /// Example: InvalidateByPrefixAsync("myapi:product") removes every product key.
    /// </summary>
    public async Task InvalidateByPrefixAsync(string prefix, CancellationToken ct = default)
    {
        var server = _redis.GetServer(_redis.GetEndPoints().First());
        var database = _redis.GetDatabase();

        // SCAN is non-blocking unlike KEYS — safe in production
        var pattern = $"{prefix}*";
        var deleted = 0;

        await foreach (var key in server.KeysAsync(pattern: pattern).WithCancellation(ct))
        {
            await database.KeyDeleteAsync(key);
            deleted++;
        }

        _logger.LogInformation(
            "Cache invalidation: deleted {Count} keys matching pattern {Pattern}",
            deleted, pattern);
    }

    /// <summary>
    /// Invalidates all product-related keys after a catalog-wide change.
    /// </summary>
    public Task InvalidateAllProductsAsync(CancellationToken ct = default)
        => InvalidateByPrefixAsync(CacheKeys.ProductsPrefix, ct);

    /// <summary>
    /// Invalidates a user's cached profile and permissions.
    /// </summary>
    public async Task InvalidateUserAsync(string userId, CancellationToken ct = default)
    {
        var database = _redis.GetDatabase();
        await Task.WhenAll(
            database.KeyDeleteAsync(CacheKeys.UserProfile(userId)),
            database.KeyDeleteAsync(CacheKeys.UserPermissions(userId))
        ).WaitAsync(ct);
    }
}