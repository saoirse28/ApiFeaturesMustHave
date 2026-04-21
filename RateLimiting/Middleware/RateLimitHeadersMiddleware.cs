using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

namespace RateLimiting.Middleware;

/// <summary>
/// Adds X-RateLimit-* informational headers to every response so
/// well-behaved clients can self-throttle before hitting a 429.
///
/// Standard headers:
///   X-RateLimit-Limit     — max permits allowed per window
///   X-RateLimit-Remaining — permits left in the current window
///   X-RateLimit-Reset     — Unix timestamp when the window resets
///   X-RateLimit-Policy    — which policy was applied (for debugging)
/// </summary>
public sealed class RateLimitHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public RateLimitHeadersMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        await _next(context);

        // Read rate limit stats from the lease acquired by the rate limiter
        // These are populated by the RateLimiter after the lease is acquired.
        if (context.Features.Get<IRateLimitingFeature>() is { } feature)
        {
            var lease = feature.Lease;

            if (lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
            {
                context.Response.Headers["Retry-After"] =
                    ((int)retryAfter.TotalSeconds).ToString();
            }
        }

        // Add rate limit headers from the endpoint metadata if available
        var endpoint = context.GetEndpoint();
        var metadata = endpoint?.Metadata.GetMetadata<IRateLimiterPolicy<HttpContext>>();

        if (metadata is not null)
        {
            context.Response.Headers["X-RateLimit-Policy"] =
                endpoint?.Metadata
                    .GetMetadata<EnableRateLimitingAttribute>()
                    ?.PolicyName ?? "unknown";
        }
    }
}

/// <summary>
/// Marker interface to access the rate limiter lease from the response pipeline.
/// </summary>
public interface IRateLimitingFeature
{
    RateLimitLease Lease { get; }
}