using Microsoft.Extensions.Caching.Distributed;
using System.Threading.RateLimiting;

namespace Resilience.RateLimiting;

/// <summary>
/// Redis-backed sliding window rate limiter for multi-instance deployments.
///
/// Problem: the built-in .NET rate limiters are in-process only.
/// If you run 3 API instances, each instance tracks its own counter,
/// so a client can make 3× the intended limit across instances.
///
/// Solution: use Redis INCR + EXPIRE to maintain a shared counter.
/// This is a simplified implementation — production use should use
/// a Lua script for atomicity.
/// </summary>
public sealed class RedisRateLimiter
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<RedisRateLimiter> _logger;

    public RedisRateLimiter(
        IDistributedCache cache,
        ILogger<RedisRateLimiter> logger)
    {
        _cache  = cache;
        _logger = logger;
    }

    /// <summary>
    /// Checks and increments a distributed sliding window counter in Redis.
    /// Returns true if the request is allowed, false if rate limited.
    /// </summary>
    public async Task<RateLimitDecision> CheckRateLimitAsync(
        string clientKey,
        string policyName,
        int permitLimit,
        TimeSpan window,
        CancellationToken ct = default)
    {
        // Window key — bucket per client per time window
        var windowStart = DateTimeOffset.UtcNow
            .ToUnixTimeSeconds() / (long)window.TotalSeconds;
        var key = $"ratelimit:{policyName}:{clientKey}:{windowStart}";

        try
        {
            var countBytes = await _cache.GetAsync(key, ct);
            var count = countBytes is null
                ? 0
                : int.Parse(System.Text.Encoding.UTF8.GetString(countBytes));

            if (count >= permitLimit)
            {
                var resetAt = DateTimeOffset.UtcNow
                    .AddSeconds(window.TotalSeconds - (DateTimeOffset.UtcNow.ToUnixTimeSeconds() % (long)window.TotalSeconds));

                _logger.LogWarning(
                    "Rate limit exceeded: client={Client}, policy={Policy}, count={Count}/{Limit}",
                    clientKey, policyName, count, permitLimit);

                return new RateLimitDecision(
                    IsAllowed: false,
                    Remaining: 0,
                    ResetAt: resetAt,
                    Limit: permitLimit);
            }

            // Increment and set expiry atomically (simplified — use Lua script for production)
            var newCount = count + 1;
            var entry = System.Text.Encoding.UTF8.GetBytes(newCount.ToString());
            await _cache.SetAsync(key, entry, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = window
            }, ct);

            return new RateLimitDecision(
                IsAllowed: true,
                Remaining: permitLimit - newCount,
                ResetAt: DateTimeOffset.UtcNow.Add(window),
                Limit: permitLimit);
        }
        catch (Exception ex)
        {
            // Redis unavailable — fail open (allow the request)
            // Prevents Redis outage from taking down the entire API
            _logger.LogWarning(ex,
                "Redis rate limiter unavailable — failing open for client {Client}", clientKey);

            return new RateLimitDecision(
                IsAllowed: true,
                Remaining: permitLimit,
                ResetAt: DateTimeOffset.UtcNow.Add(window),
                Limit: permitLimit);
        }
    }
}

public sealed record RateLimitDecision(
    bool IsAllowed,
    int Remaining,
    DateTimeOffset ResetAt,
    int Limit);