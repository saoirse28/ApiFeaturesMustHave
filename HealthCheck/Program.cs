using HealthCheckMetric.HealthChecks;
using HealthCheckMetric.Options;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using HealthCheckMetric.Metrics;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOptions<ConnectionStringOptions>()
    .BindConfiguration("ConnectionStrings")
    .ValidateDataAnnotations() // Optional: Validate properties using [Required] etc.
    .ValidateOnStart();       // Optional: Fail early if config is missing

builder.Services.AddSingleton<AppMetrics>();

builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>(
        name: "database",
        failureStatus: HealthStatus.Unhealthy,
        tags: ["ready", "db"])

    .AddCheck<ExternalApiHealthCheck>(
        name: "payment-api",
        failureStatus: HealthStatus.Degraded,
        tags: ["ready", "external"])

    // SQL Server (built-in from AspNetCore.HealthChecks.SqlServer)
    .AddSqlServer(
        connectionString: builder.Configuration.GetConnectionString("Mssql")!,
        name: "sql-server",
        failureStatus: HealthStatus.Unhealthy,
        tags: ["ready", "db"])

    // Disk check — tagged "live" (used for liveness probe)
    .AddCheck<DiskSpaceHealthCheck>(
        name: "disk-space",
        failureStatus: HealthStatus.Unhealthy,
        tags: ["live"])

    // Redis (built-in from AspNetCore.HealthChecks.Redis)
    .AddRedis(
        redisConnectionString: builder.Configuration.GetConnectionString("Redis")!,
        name: "redis-builtin",
        failureStatus: HealthStatus.Degraded,
        tags: ["ready", "cache"])

    // External URL check
    .AddUrlGroup(
        uri: new Uri("https://claude.com/"),
        name: "external-api",
        failureStatus: HealthStatus.Degraded,
        tags: ["ready", "external"]);

// ── Health Check UI (optional dashboard) ────────────────────────────────────

builder.Services.AddHealthChecksUI(opts =>
{
    opts.SetEvaluationTimeInSeconds(15); // Poll every 15s
    opts.MaximumHistoryEntriesPerEndpoint(50);
    opts.AddHealthCheckEndpoint("API Health", "/health");
})
.AddInMemoryStorage();

// ── OpenTelemetry Metrics ────────────────────────────────────────────────────

builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService(
        serviceName: "MyApiHealthCheck",
        serviceVersion: "1.0.0"))
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()         // HTTP request metrics
        .AddRuntimeInstrumentation()            // GC, thread pool, memory
        .AddMeter("MyApiHealthCheckMetric.Metrics")   // Custom application meters
        .AddPrometheusExporter());              // /metrics endpoint

builder.Services.AddControllers();

var app = builder.Build();

// ── Middleware Pipeline ──────────────────────────────────────────────────────

// Liveness probe — is the process alive?
// Returns 200 even if dependencies are down (process is running)
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live"),
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

// Readiness probe — are all dependencies ready?
// Returns 200 only when all tagged "ready" checks pass
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

// Full health report — all checks, rich JSON
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

// Prometheus metrics scraping endpoint
app.MapPrometheusScrapingEndpoint("/metrics");

// Optional: HealthChecks UI dashboard at /healthchecks-ui
app.MapHealthChecksUI(opts => opts.UIPath = "/healthchecks-ui");

app.MapControllers();
app.Run();