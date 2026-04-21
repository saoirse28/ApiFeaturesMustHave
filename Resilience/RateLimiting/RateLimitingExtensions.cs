using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

namespace Resilience.RateLimiting;

public static class RateLimitingExtensions
{
    /// <summary>
    /// Registers all rate limit policies and the global limiter.
    /// Called once from Program.cs.
    /// </summary>
    public static IServiceCollection AddRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var settings = configuration
            .GetSection("RateLimiting")
            .Get<RateLimitSettings>() ?? new RateLimitSettings();

        services.AddRateLimiter(opts =>
        {
            // ── Global 429 response writer ─────────────────────────────────
            opts.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            opts.OnRejected          = RateLimitResponseWriter.WriteAsync;

            // ── Per-client sliding window (standard authenticated users) ───
            AddPerClientPolicy(opts, settings);

            // ── Trusted partner — elevated quota ───────────────────────────
            AddTrustedPartnerPolicy(opts, settings);

            // ── Premium tier — highest quota ───────────────────────────────
            AddPremiumTierPolicy(opts, settings);

            // ── Authentication endpoints — strict brute-force protection ───
            AddAuthenticationPolicy(opts, settings);

            // ── Sensitive operations (password reset, OTP) ─────────────────
            AddSensitiveOperationPolicy(opts, settings);

            // ── Search — token bucket allows short bursts ──────────────────
            AddSearchPolicy(opts, settings);

            // ── Webhooks — concurrency limiter (not time-based) ───────────
            AddWebhookPolicy(opts, settings);

            // ── Anonymous public endpoints — IP-based fixed window ─────────
            AddAnonymousPublicPolicy(opts, settings);

            // ── Global limiter — DDoS protection across all requests ───────
            AddGlobalLimiter(opts);
        });

        return services;
    }

    // ── Policy definitions ─────────────────────────────────────────────────────

    private static void AddPerClientPolicy(
        RateLimiterOptions opts, RateLimitSettings settings)
    {
        var s = settings.PerClient;

        // Sliding window: smoother than fixed window, prevents boundary bursting.
        // Partitioned by client key (user ID > API key > IP).
        opts.AddPolicy(RateLimitPolicies.PerClient, context =>
            RateLimitPartition.GetSlidingWindowLimiter(
                partitionKey: context.GetClientKey(),
                factory: _ => new SlidingWindowRateLimiterOptions
                {
                    PermitLimit      = s.PermitLimit,
                    Window           = TimeSpan.FromSeconds(s.WindowSeconds),
                    SegmentsPerWindow = s.SegmentsPerWindow,
                    QueueLimit       = s.QueueLimit,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    AutoReplenishment = true
                }));
    }

    private static void AddTrustedPartnerPolicy(
        RateLimiterOptions opts, RateLimitSettings settings)
    {
        var s = settings.TrustedPartner;

        opts.AddPolicy(RateLimitPolicies.TrustedPartner, context =>
            RateLimitPartition.GetSlidingWindowLimiter(
                partitionKey: context.GetClientKey(),
                factory: _ => new SlidingWindowRateLimiterOptions
                {
                    PermitLimit      = s.PermitLimit,
                    Window           = TimeSpan.FromSeconds(s.WindowSeconds),
                    SegmentsPerWindow = s.SegmentsPerWindow,
                    QueueLimit       = s.QueueLimit,
                    AutoReplenishment = true
                }));
    }

    private static void AddPremiumTierPolicy(
        RateLimiterOptions opts, RateLimitSettings settings)
    {
        var s = settings.PremiumTier;

        opts.AddPolicy(RateLimitPolicies.PremiumTier, context =>
            RateLimitPartition.GetSlidingWindowLimiter(
                partitionKey: context.GetClientKey(),
                factory: _ => new SlidingWindowRateLimiterOptions
                {
                    PermitLimit      = s.PermitLimit,
                    Window           = TimeSpan.FromSeconds(s.WindowSeconds),
                    SegmentsPerWindow = s.SegmentsPerWindow,
                    QueueLimit       = s.QueueLimit,
                    AutoReplenishment = true
                }));
    }

    private static void AddAuthenticationPolicy(
        RateLimiterOptions opts, RateLimitSettings settings)
    {
        var s = settings.Authentication;

        // Fixed window on IP — login is always anonymous at request time.
        // Small permit limit with NO queuing to immediately reject brute-force.
        opts.AddPolicy(RateLimitPolicies.Authentication, context =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: $"auth:{context.GetClientIp()}",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit      = s.PermitLimit,
                    Window           = TimeSpan.FromMinutes(s.WindowMinutes),
                    QueueLimit       = 0,   // no queuing — reject immediately
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    AutoReplenishment = true
                }));
    }

    private static void AddSensitiveOperationPolicy(
        RateLimiterOptions opts, RateLimitSettings settings)
    {
        var s = settings.SensitiveOperation;

        opts.AddPolicy(RateLimitPolicies.SensitiveOperation, context =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: $"sensitive:{context.GetClientKey()}",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit      = s.PermitLimit,
                    Window           = TimeSpan.FromHours(s.WindowHours),
                    QueueLimit       = 0,
                    AutoReplenishment = true
                }));
    }

    private static void AddSearchPolicy(
        RateLimiterOptions opts, RateLimitSettings settings)
    {
        var s = settings.Search;

        // Token bucket: replenishes tokens continuously, allows short bursts
        // (client can fire up to TokenLimit requests instantly, then is rate-limited).
        opts.AddPolicy(RateLimitPolicies.Search, context =>
            RateLimitPartition.GetTokenBucketLimiter(
                partitionKey: context.GetClientKey(),
                factory: _ => new TokenBucketRateLimiterOptions
                {
                    TokenLimit                  = s.TokenLimit,
                    ReplenishmentPeriod         = TimeSpan.FromSeconds(s.TokensPerPeriodSeconds),
                    TokensPerPeriod             = s.TokensPerPeriod,
                    QueueLimit                  = s.QueueLimit,
                    QueueProcessingOrder        = QueueProcessingOrder.OldestFirst,
                    AutoReplenishment           = true
                }));
    }

    private static void AddWebhookPolicy(
        RateLimiterOptions opts, RateLimitSettings settings)
    {
        var s = settings.Webhook;

        // Concurrency limiter: limits simultaneous requests (not time-based).
        // Ideal for long-running operations — prevents resource exhaustion.
        opts.AddPolicy(RateLimitPolicies.Webhook, context =>
            RateLimitPartition.GetConcurrencyLimiter(
                partitionKey: context.GetClientKey(),
                factory: _ => new ConcurrencyLimiterOptions
                {
                    PermitLimit          = s.PermitLimit,
                    QueueLimit           = s.QueueLimit,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                }));
    }

    private static void AddAnonymousPublicPolicy(
        RateLimiterOptions opts, RateLimitSettings settings)
    {
        var s = settings.AnonymousPublic;

        opts.AddPolicy(RateLimitPolicies.AnonymousPublic, context =>
            RateLimitPartition.GetSlidingWindowLimiter(
                partitionKey: $"anon:{context.GetClientIp()}",
                factory: _ => new SlidingWindowRateLimiterOptions
                {
                    PermitLimit      = s.PermitLimit,
                    Window           = TimeSpan.FromSeconds(s.WindowSeconds),
                    SegmentsPerWindow = s.SegmentsPerWindow,
                    QueueLimit       = 0,
                    AutoReplenishment = true
                }));
    }

    private static void AddGlobalLimiter(RateLimiterOptions opts)
    {
        // Global limiter: applied to EVERY request before any policy.
        // Acts as a DDoS circuit breaker — keeps the server alive under attack.
        // Loose limit — per-client policies enforce finer-grained control.
        opts.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
            RateLimitPartition.GetTokenBucketLimiter(
                partitionKey: "global",
                factory: _ => new TokenBucketRateLimiterOptions
                {
                    TokenLimit          = 5000,
                    ReplenishmentPeriod = TimeSpan.FromSeconds(10),
                    TokensPerPeriod     = 1000,
                    QueueLimit          = 0,
                    AutoReplenishment   = true
                }));
    }

    /// <summary>
    /// Activates the rate limiter middleware in the request pipeline.
    /// MUST be called before UseAuthentication and MapControllers.
    /// </summary>
    public static WebApplication UseRateLimiting(this WebApplication app)
    {
        app.UseRateLimiter();
        return app;
    }
}