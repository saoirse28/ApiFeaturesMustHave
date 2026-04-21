using Serilog;
using Serilog.Enrichers.Span;
using Serilog.Events;
using Serilog.Formatting.Compact;

namespace ProperLogging.Logging;

public static class SerilogConfiguration
{
    /// <summary>
    /// Builds a fully configured Serilog logger.
    /// Called before WebApplication.CreateBuilder so that startup
    /// errors are also captured (bootstrap logger pattern).
    /// </summary>
    public static LoggerConfiguration Build(
        IConfiguration configuration,
        IHostEnvironment environment,
        LoggerConfiguration config)
    {

            // ── Read overrides from appsettings ────────────────────────────
            config.ReadFrom.Configuration(configuration)

            // ── Minimum level ──────────────────────────────────────────────
            .MinimumLevel.Is(environment.IsDevelopment()
                ? LogEventLevel.Debug
                : LogEventLevel.Information)

            // Silence noisy framework namespaces in production
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
            .MinimumLevel.Override("System.Net.Http.HttpClient", LogEventLevel.Warning)
            .MinimumLevel.Override("HealthChecks", LogEventLevel.Warning)

            // ── Enrichers — add properties to EVERY log event ─────────────
            .Enrich.FromLogContext()                  // properties pushed via LogContext.PushProperty
            .Enrich.WithMachineName()                 // MachineName
            .Enrich.WithEnvironmentName()             // EnvironmentName
            .Enrich.WithThreadId()                    // ThreadId
            .Enrich.WithCorrelationId()               // CorrelationId (from header)
            .Enrich.WithSpan()                        // TraceId + SpanId from Activity
            .Enrich.WithProperty(                     // App version from assembly
                LoggingConstants.AppVersion,
                typeof(SerilogConfiguration).Assembly.GetName().Version?.ToString() ?? "unknown")

            // ── Sinks ──────────────────────────────────────────────────────

            // Console: human-readable in dev, compact JSON in prod
            .WriteTo.Conditional(
                condition: _ => environment.IsDevelopment(),
                configureSink: sink => sink.Console(
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}"))
            .WriteTo.Conditional(
                condition: _ => !environment.IsDevelopment(),
                configureSink: sink => sink.Console(new CompactJsonFormatter()))

            // File: rolling daily, compact JSON, kept for 30 days
            .WriteTo.File(
                formatter: new CompactJsonFormatter(),
                path: "logs/myapi-.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                fileSizeLimitBytes: 100 * 1024 * 1024,  // 100 MB per file
                rollOnFileSizeLimit: true,
                shared: false)

            // Seq: structured log server (dev + staging)
            .WriteTo.Conditional(
                condition: _ => !string.IsNullOrEmpty(configuration["Seq:Url"]),
                configureSink: sink => sink.Seq(
                    serverUrl: configuration["Seq:Url"]!,
                    apiKey: configuration["Seq:ApiKey"],
                    restrictedToMinimumLevel: LogEventLevel.Debug))

            // OpenTelemetry (production — ship to Grafana Loki / Datadog)
            .WriteTo.Conditional(
                condition: _ => !environment.IsDevelopment(),
                configureSink: sink => sink.OpenTelemetry(opts =>
                {
                    opts.Endpoint = configuration["OpenTelemetry:Endpoint"]
                        ?? "http://localhost:4317";
                    opts.ResourceAttributes = new Dictionary<string, object>
                    {
                        ["service.name"]    = "MyApi",
                        ["service.version"] = "1.0.0"
                    };
                }));

        return config;
    }
}