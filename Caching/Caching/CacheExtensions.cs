namespace Caching.Caching;

public static class CacheExtensions
{
    public static IServiceCollection AddCaching(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ── Layer 1: In-memory cache (L1) ──────────────────────────────────────
        services.AddMemoryCache(opts =>
        {
            opts.SizeLimit              = 1024;            // max 1024 entries
            opts.CompactionPercentage   = 0.25;            // evict 25% when full
            opts.ExpirationScanFrequency = TimeSpan.FromMinutes(5);
        });

        // ── Layer 2: Distributed Redis cache (L2) ──────────────────────────────
        services.AddStackExchangeRedisCache(opts =>
        {
            opts.Configuration         = configuration["CacheConnection"];
            opts.InstanceName          = "myapi:";        // key prefix in Redis
            opts.ConfigurationOptions  = new StackExchange.Redis.ConfigurationOptions
            {
                AbortOnConnectFail = false,               // don't crash if Redis is down
                ConnectTimeout     = 3000,
                SyncTimeout        = 1000,
                ReconnectRetryPolicy = new StackExchange.Redis.ExponentialRetry(500),
                EndPoints = { configuration["CacheConnection"] }
            };
        });

        // ── Layer 3: HTTP Output Cache (L3) ────────────────────────────────────
        services.AddOutputCache(opts =>
        {
            // Base policy — applies to all cached endpoints unless overridden
            opts.AddBasePolicy(builder => builder
                .Expire(TimeSpan.FromSeconds(30))
                .With(ctx => ctx.HttpContext.Request.Method == HttpMethods.Get)
                .Tag("all"));

            // Products — vary by query params, tagged for bulk eviction
            opts.AddPolicy(CachePolicies.Products, builder => builder
                .Expire(TimeSpan.FromMinutes(5))
                .SetVaryByQuery("category", "page", "pageSize", "sort")
                .SetVaryByHeader("Accept-Language")
                .Tag(CachePolicies.Products)
                .Tag("all"));

            // Categories — long TTL, simple
            opts.AddPolicy(CachePolicies.Categories, builder => builder
                .Expire(TimeSpan.FromHours(1))
                .Tag(CachePolicies.Categories)
                .Tag("all"));

            // User-specific — vary by authorization header (JWT)
            opts.AddPolicy(CachePolicies.UserProfile, builder => builder
                .Expire(TimeSpan.FromMinutes(10))
                .SetVaryByHeader("Authorization")
                .Tag(CachePolicies.UserProfile));

            // Explicit no-cache policy for write endpoints
            opts.AddPolicy(CachePolicies.NoCache, builder => builder
                .NoCache());
        });

        return services;
    }
}