namespace Resilience.RateLimiting;

/// <summary>
/// Centralizes all rate limit policy name strings.
///
/// A typo in a policy name silently applies NO rate limiting —
/// the middleware ignores unknown policy names without throwing.
/// Always reference these constants, never inline string literals.
/// </summary>
public static class RateLimitPolicies
{
    // ── Per-authenticated-client policies ────────────────────────────────────
    /// <summary>Standard API usage — 100 req/min sliding window, per user ID.</summary>
    public const string PerClient = "per-client";

    /// <summary>Elevated quota for trusted partners — 500 req/min.</summary>
    public const string TrustedPartner = "trusted-partner";

    /// <summary>Premium tier — 1000 req/min.</summary>
    public const string PremiumTier = "premium-tier";

    // ── Endpoint-specific policies ────────────────────────────────────────────
    /// <summary>Login / token endpoints — 5 attempts per 15 min per IP.</summary>
    public const string Authentication = "authentication";

    /// <summary>Password reset / OTP — 3 requests per hour per IP.</summary>
    public const string SensitiveOperation = "sensitive-operation";

    /// <summary>Search / autocomplete — 30 req/min, token bucket for burst.</summary>
    public const string Search = "search";

    /// <summary>Webhook delivery endpoints — concurrency limiter, max 10 parallel.</summary>
    public const string Webhook = "webhook";

    // ── Global / anonymous policies ───────────────────────────────────────────
    /// <summary>Anonymous public endpoints — 20 req/min per IP.</summary>
    public const string AnonymousPublic = "anonymous-public";

    /// <summary>Health/metrics endpoints — explicitly no rate limiting.</summary>
    public const string None = "none";
}