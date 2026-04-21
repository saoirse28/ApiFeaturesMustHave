using ProperLogging.Logging;
using Serilog.Context;
using System.Diagnostics;

namespace ProperLogging.Middleware;

/// <summary>
/// Logs every HTTP request with timing, status code, and contextual properties.
/// Uses Serilog's built-in UseSerilogRequestLogging() under the hood but extends
/// it with custom properties: UserId, ClientIp, and slow-request warnings.
///
/// Place AFTER CorrelationIdMiddleware so CorrelationId is already in LogContext.
/// Place BEFORE UseAuthentication so UserId is set by the time the log fires.
/// </summary>
public sealed class RequestLoggingMiddleware
{
    private const int SlowRequestThresholdMs = 1000;
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(
        RequestDelegate next,
        ILogger<RequestLoggingMiddleware> logger)
    {
        _next   = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
         //Skip health and metrics endpoints — they're noise in production logs
        if (IsNoiseEndpoint(context.Request.Path))
        {
            await _next(context);
            return;
        }

        var sw = Stopwatch.StartNew();

        try
        {
            await _next(context);
        }
        finally
        {
            sw.Stop();

            var statusCode = context.Response.StatusCode;
            var elapsedMs = sw.ElapsedMilliseconds;
            var method = context.Request.Method;
            var path = context.Request.Path.Value ?? "/";
            var clientIp = context.Connection.RemoteIpAddress?.ToString();
            var userId = context.User?.FindFirst("sub")?.Value;

            // Push request-level properties so they appear on THIS log event
            using (LogContext.PushProperty(LoggingConstants.StatusCode, statusCode))
            using (LogContext.PushProperty(LoggingConstants.ElapsedMs, elapsedMs))
            using (LogContext.PushProperty(LoggingConstants.ClientIp, clientIp ?? "unknown"))
            using (LogContext.PushProperty(LoggingConstants.UserId, userId ?? "anonymous"))
            using (LogContext.PushProperty(LoggingConstants.RequestMethod, method))
            using (LogContext.PushProperty(LoggingConstants.RequestPath, path))
            {
                if (elapsedMs > SlowRequestThresholdMs)
                {
                    _logger.LogWarning(
                        "Slow request: {Method} {Path} → {StatusCode} in {ElapsedMs}ms",
                        method, path, statusCode, elapsedMs);
                }
                else if (statusCode >= 500)
                {
                    _logger.LogError(
                        "Request failed: {Method} {Path} → {StatusCode} in {ElapsedMs}ms",
                        method, path, statusCode, elapsedMs);
                }
                else if (statusCode >= 400)
                {
                    _logger.LogWarning(
                        "Client error: {Method} {Path} → {StatusCode} in {ElapsedMs}ms",
                        method, path, statusCode, elapsedMs);
                }
                else
                {
                    _logger.LogInformation(
                        "{Method} {Path} → {StatusCode} in {ElapsedMs}ms",
                        method, path, statusCode, elapsedMs);
                }
            }
        }
    }

    private static bool IsNoiseEndpoint(PathString path) =>
        path.StartsWithSegments("/health") ||
        path.StartsWithSegments("/metrics") ||
        path.StartsWithSegments("/favicon.ico");
}
