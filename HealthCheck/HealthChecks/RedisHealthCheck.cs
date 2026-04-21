using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace HealthCheckMetric.HealthChecks;

public class RedisHealthCheck : IHealthCheck
{
    private readonly IConnectionMultiplexer _redis;

    public RedisHealthCheck(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var db = _redis.GetDatabase();
            var sw = System.Diagnostics.Stopwatch.StartNew();

            await db.PingAsync();
            sw.Stop();

            var server = _redis.GetServer(_redis.GetEndPoints().First());
            var info = await server.InfoAsync("server");

            var data = new Dictionary<string, object>
            {
                ["pingMs"] = sw.ElapsedMilliseconds,
                ["connected"] = _redis.IsConnected,
                ["endpoints"] = _redis.GetEndPoints().Length
            };

            return sw.ElapsedMilliseconds > 500
                ? HealthCheckResult.Degraded("Redis slow ping", data: data)
                : HealthCheckResult.Healthy("Redis OK", data: data);
        }
        catch (RedisConnectionException ex)
        {
            return HealthCheckResult.Unhealthy("Redis unreachable", ex);
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Redis error", ex);
        }
    }
}