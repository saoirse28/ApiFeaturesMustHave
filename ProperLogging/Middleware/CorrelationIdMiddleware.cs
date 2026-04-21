using ProperLogging.Logging;
using Serilog.Context;

namespace ProperLogging.Middleware;

/// <summary>
/// Ensures every request has a unique correlation ID:
///   1. Reads X-Correlation-Id from incoming request header.
///   2. Generates a new compact UUID if absent.
///   3. Pushes it into Serilog's LogContext so every log event
///      in this request automatically includes CorrelationId.
///   4. Sets HttpContext.TraceIdentifier so it appears in ASP.NET logs.
///   5. Echoes the ID back in the response header.
/// </summary>
public sealed class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-Id";
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[HeaderName].FirstOrDefault()
            ?? Guid.NewGuid().ToString("N");

        context.Items[HeaderName]    = correlationId;
        context.TraceIdentifier      = correlationId;

        // Push into Serilog — all log calls on this thread get CorrelationId automatically
        using (LogContext.PushProperty(LoggingConstants.CorrelationId, correlationId))
        {
            context.Response.OnStarting(() =>
            {
                context.Response.Headers.TryAdd(HeaderName, correlationId);
                return Task.CompletedTask;
            });

            await _next(context);
        }
    }
}