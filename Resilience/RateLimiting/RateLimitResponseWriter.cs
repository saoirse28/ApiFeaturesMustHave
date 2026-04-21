using Microsoft.AspNetCore.RateLimiting;
using System.Text.Json;
using System.Threading.RateLimiting;

namespace Resilience.RateLimiting;

/// <summary>
/// Writes a structured JSON body for 429 Too Many Requests responses.
/// Also adds standard rate-limit headers so clients know when to retry.
/// </summary>
public static class RateLimitResponseWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static async ValueTask WriteAsync(OnRejectedContext context, CancellationToken ct)
    {
        var httpContext = context.HttpContext;
        httpContext.Response.StatusCode  = StatusCodes.Status429TooManyRequests;
        httpContext.Response.ContentType = "application/problem+json";

        // Standard Retry-After header (seconds until the window resets)
        var retryAfter = context.Lease.TryGetMetadata(
            MetadataName.RetryAfter, out var retryAfterValue)
            ? (int)retryAfterValue.TotalSeconds
            : 60;

        httpContext.Response.Headers["Retry-After"]      = retryAfter.ToString();
        httpContext.Response.Headers["X-RateLimit-Reset"] =
            DateTimeOffset.UtcNow.AddSeconds(retryAfter).ToUnixTimeSeconds().ToString();

        var body = new
        {
            type = "https://api.myapp.com/errors/rate-limit-exceeded",
            title = "Too Many Requests",
            status = 429,
            detail = $"Rate limit exceeded. Please retry after {retryAfter} seconds.",
            retryAfterSeconds = retryAfter,
            correlationId = httpContext.Request.Headers["X-Correlation-Id"]
                                .FirstOrDefault() ?? httpContext.TraceIdentifier,
            timestamp = DateTimeOffset.UtcNow
        };

        await httpContext.Response.WriteAsJsonAsync(body, JsonOptions, ct);
    }
}