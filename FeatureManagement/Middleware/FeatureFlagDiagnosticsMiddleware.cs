using Microsoft.FeatureManagement;

namespace FeatureManagement.Middleware;

/// <summary>
/// Logs which feature flags are active for each request.
/// Useful for debugging rollout issues — you can see exactly which
/// flags were evaluated and their result for a given user/request.
///
/// Only runs in Development and Staging to avoid log noise in production.
/// </summary>
public sealed class FeatureFlagDiagnosticsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<FeatureFlagDiagnosticsMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    // The set of flags to evaluate and log per request
    private static readonly string[] TrackedFlags =
    [
        FeatureFlags.FeatureFlags.NewCheckoutFlow,
        FeatureFlags.FeatureFlags.OneClickCheckout,
        FeatureFlags.FeatureFlags.BuyNowPayLater,
        FeatureFlags.FeatureFlags.NewRecommendationEngine,
        FeatureFlags.FeatureFlags.SemanticSearch,
        FeatureFlags.FeatureFlags.BetaDashboard,
        FeatureFlags.FeatureFlags.MaintenanceMode
    ];

    public FeatureFlagDiagnosticsMiddleware(
        RequestDelegate next,
        ILogger<FeatureFlagDiagnosticsMiddleware> logger,
        IHostEnvironment environment)
    {
        _next        = next;
        _logger      = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context, IFeatureManager features)
    {
        if (!_environment.IsDevelopment() && !_environment.IsStaging())
        {
            await _next(context);
            return;
        }

        var flagStates = new Dictionary<string, bool>();

        foreach (var flag in TrackedFlags)
            flagStates[flag] = await features.IsEnabledAsync(flag);

        var activeFlags = flagStates
            .Where(kv => kv.Value)
            .Select(kv => kv.Key)
            .ToList();

        if (activeFlags.Count > 0)
        {
            _logger.LogDebug(
                "Active feature flags for {Method} {Path}: [{Flags}]",
                context.Request.Method,
                context.Request.Path,
                string.Join(", ", activeFlags));
        }

        // Add to response headers for easy debugging in browser / Postman
        context.Response.OnStarting(() =>
        {
            if (activeFlags.Count > 0)
                context.Response.Headers["X-Active-Features"] =
                    string.Join(",", activeFlags);
            return Task.CompletedTask;
        });

        await _next(context);
    }
}