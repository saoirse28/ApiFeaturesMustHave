using System.Security.Claims;

namespace Resilience.RateLimiting;

/// <summary>
/// Extracts a stable, unique client identity key from an HTTP request.
///
/// Priority order:
///   1. Authenticated user ID (JWT sub claim) — most accurate, survives NAT
///   2. API key from X-Api-Key header — for machine-to-machine clients
///   3. Client tier claim — adjusts quota per subscription plan
///   4. Remote IP address — fallback for anonymous requests
///   5. "anonymous" — last resort
///
/// Never use IP address alone for authenticated endpoints — IP is unreliable
/// behind NAT, corporate proxies, and mobile networks where thousands of
/// users share a single external IP.
/// </summary>
public static class ClientKeyProvider
{
    private const string ApiKeyHeader = "X-Api-Key";
    private const string ForwardedForHeader = "X-Forwarded-For";
    private const string RealIpHeader = "X-Real-IP";

    /// <summary>
    /// Returns a partition key for per-authenticated-user rate limiting.
    /// Falls back to IP if the user is not authenticated.
    /// </summary>
    public static string GetClientKey(this HttpContext context)
    {
        // 1. Authenticated user ID (JWT sub / NameIdentifier)
        var userId = context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? context.User?.FindFirstValue("sub");
        if (!string.IsNullOrEmpty(userId))
            return $"user:{userId}";

        // 2. API key from header
        var apiKey = context.Request.Headers[ApiKeyHeader].FirstOrDefault();
        if (!string.IsNullOrEmpty(apiKey))
            return $"apikey:{apiKey}";

        // 3. Real IP (handles reverse-proxy scenarios)
        return $"ip:{GetClientIp(context)}";
    }

    /// <summary>
    /// Returns the client's subscription tier for tiered rate limiting.
    /// </summary>
    public static string GetClientTier(this HttpContext context)
    {
        // Read tier from a custom JWT claim (set during token issuance)
        var tier = context.User?.FindFirstValue("subscription_tier");
        return tier?.ToLowerInvariant() switch
        {
            "premium" => RateLimitPolicies.PremiumTier,
            "partner" => RateLimitPolicies.TrustedPartner,
            _ => RateLimitPolicies.PerClient
        };
    }

    /// <summary>
    /// Returns the real client IP, respecting X-Forwarded-For from trusted proxies.
    /// IMPORTANT: Only trust X-Forwarded-For if your infrastructure always sets it.
    /// If the header can be spoofed by clients, use RemoteIpAddress only.
    /// </summary>
    public static string GetClientIp(this HttpContext context)
    {
        // X-Forwarded-For: client, proxy1, proxy2 — take the first (leftmost) IP
        var forwardedFor = context.Request.Headers[ForwardedForHeader]
            .FirstOrDefault()
            ?.Split(',')
            .FirstOrDefault()
            ?.Trim();

        if (!string.IsNullOrEmpty(forwardedFor))
            return forwardedFor;

        var realIp = context.Request.Headers[RealIpHeader].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
            return realIp;

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}