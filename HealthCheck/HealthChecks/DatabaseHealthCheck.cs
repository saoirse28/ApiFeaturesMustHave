using HealthCheckMetric.Options;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace HealthCheckMetric.HealthChecks;

public class DatabaseHealthCheck : IHealthCheck
{
    private readonly ILogger<DatabaseHealthCheck> _logger;
    private readonly ConnectionStringOptions _config;
    public DatabaseHealthCheck(
        IOptions<ConnectionStringOptions> options,
        ILogger<DatabaseHealthCheck> logger)
    {
        _config = options.Value;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var connectionString = _config.Mssql;
        var sw = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1";
            command.CommandTimeout = 5; // seconds
            await command.ExecuteScalarAsync(cancellationToken);

            sw.Stop();

            var data = new Dictionary<string, object>
            {
                ["responseTimeMs"] = sw.ElapsedMilliseconds,
                ["server"] = connection.DataSource
            };

            // Degraded if response is slow but connected
            if (sw.ElapsedMilliseconds > 1000)
            {
                return HealthCheckResult.Degraded(
                    description: $"Database responding slowly ({sw.ElapsedMilliseconds}ms)",
                    data: data);
            }

            return HealthCheckResult.Healthy(
                description: $"Database OK ({sw.ElapsedMilliseconds}ms)",
                data: data);
        }
        catch (SqlException ex)
        {
            sw.Stop();
            _logger.LogError(ex, "Database health check failed after {Ms}ms", sw.ElapsedMilliseconds);

            return HealthCheckResult.Unhealthy(
                description: "Database connection failed",
                exception: ex,
                data: new Dictionary<string, object>
                {
                    ["responseTimeMs"] = sw.ElapsedMilliseconds,
                    ["errorCode"] = ex.Number
                });
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "Unexpected database health check error");

            return HealthCheckResult.Unhealthy(
                description: "Unexpected database error",
                exception: ex);
        }
    }
}