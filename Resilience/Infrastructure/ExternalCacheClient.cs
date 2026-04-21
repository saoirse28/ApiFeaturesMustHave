using Polly;
using Polly.CircuitBreaker;
using Polly.Registry;
using Resilience.Resilience;

namespace Resilience.Infrastructure;

/// <summary>
/// Demonstrates resilience for Redis cache calls.
/// Key design: if the circuit is open (Redis is down),
/// the cache client returns null silently — the caller falls
/// back to the database without any exception propagating.
/// </summary>
public sealed class ExternalCacheClient
{
    private readonly StackExchange.Redis.IDatabase _redis;
    private readonly ResiliencePipeline _pipeline;
    private readonly ILogger<ExternalCacheClient> _logger;

    public ExternalCacheClient(
        StackExchange.Redis.IConnectionMultiplexer multiplexer,
        ResiliencePipelineProvider<string> pipelineProvider,
        ILogger<ExternalCacheClient> logger)
    {
        _redis    = multiplexer.GetDatabase();
        _pipeline = pipelineProvider.GetPipeline(ResiliencePipelineNames.Redis);
        _logger   = logger;
    }

    public async Task<string?> GetAsync(string key, CancellationToken ct = default)
    {
        try
        {
            return await _pipeline.ExecuteAsync(
                async token => (string?)await _redis.StringGetAsync(key),
                ct);
        }
        catch (BrokenCircuitException)
        {
            // Redis is known-down — degrade silently, caller uses DB
            _logger.LogWarning("Redis circuit open — cache unavailable for key {Key}", key);
            return null;
        }
    }

    public async Task SetAsync(
        string key,
        string value,
        TimeSpan expiry,
        CancellationToken ct = default)
    {
        try
        {
            await _pipeline.ExecuteAsync(
                async token => await _redis.StringSetAsync(key, value, expiry),
                ct);
        }
        catch (BrokenCircuitException)
        {
            _logger.LogWarning("Redis circuit open — skipping cache write for key {Key}", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken ct = default)
    {
        try
        {
            await _pipeline.ExecuteAsync(
                async token => await _redis.KeyDeleteAsync(key),
                ct);
        }
        catch (BrokenCircuitException)
        {
            _logger.LogWarning("Redis circuit open — skipping key deletion for {Key}", key);
        }
    }
}