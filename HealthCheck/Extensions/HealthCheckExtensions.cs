using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace HealthCheckMetric.Extensions;

public static class HealthCheckExtensions
{
    /// <summary>
    /// Maps the three standard health endpoints:
    ///   /health/live  — liveness probe (Kubernetes)
    ///   /health/ready — readiness probe (Kubernetes)
    ///   /health       — full JSON report (monitoring dashboards)
    /// </summary>
    public static WebApplication MapHealthCheckEndpoints(this WebApplication app)
    {
        // Liveness: only check that the process is running (no dependency checks)
        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("live"),
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse,
            AllowCachingResponses = false
        }).WithMetadata(new Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute());

        // Readiness: all dependencies must be up
        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready"),
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse,
            AllowCachingResponses = false
        }).WithMetadata(new Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute());

        // Full report: every registered check
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse,
            AllowCachingResponses = false,
            ResultStatusCodes =
            {
                [HealthStatus.Healthy]   = StatusCodes.Status200OK,
                [HealthStatus.Degraded]  = StatusCodes.Status200OK,  // still 200, not 503
                [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
            }
        }).WithMetadata(new Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute());

        return app;
    }
}