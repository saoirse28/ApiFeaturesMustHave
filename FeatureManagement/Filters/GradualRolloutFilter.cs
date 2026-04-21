using Microsoft.FeatureManagement;
using System.Security.Claims;

namespace FeatureManagement.Filters;

/// <summary>
/// Custom filter for deterministic gradual rollout based on user ID hash.
///
/// Unlike PercentageFilter (random per request), this filter gives the
/// same user a consistent experience — they are always in or out of
/// the rollout cohort. Eliminates feature flickering across sessions.
///
/// Algorithm: SHA-256(userId + featureName) % 100 < rolloutPercentage
///
/// Usage in appsettings.json:
/// "NewRecommendationEngine": {
///   "EnabledFor": [{
///     "Name": "GradualRollout",
///     "Parameters": { "Percentage": 25, "Salt": "reco-engine-v2" }
///   }]
/// }
/// </summary>
[FilterAlias("GradualRollout")]
public sealed class GradualRolloutFilter : IFeatureFilter
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public GradualRolloutFilter(IHttpContextAccessor httpContextAccessor)
        => _httpContextAccessor = httpContextAccessor;

    public Task<bool> EvaluateAsync(FeatureFilterEvaluationContext context)
    {
        var settings = context.Parameters.Get<GradualRolloutSettings>()
            ?? new GradualRolloutSettings();

        if (settings.Percentage <= 0) return Task.FromResult(false);
        if (settings.Percentage >= 100) return Task.FromResult(true);

        var httpContext = _httpContextAccessor.HttpContext;
        var userId = httpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? httpContext?.User.FindFirstValue("sub")
                  ?? httpContext?.Connection.RemoteIpAddress?.ToString()
                  ?? "anonymous";

        // Deterministic hash — same user always gets same result
        var salt = settings.Salt ?? context.FeatureName;
        var input = $"{userId}:{salt}";
        var hash = System.Security.Cryptography.SHA256.HashData(
                          System.Text.Encoding.UTF8.GetBytes(input));
        var bucket = BitConverter.ToUInt32(hash, 0) % 100;

        return Task.FromResult(bucket < settings.Percentage);
    }

    private sealed class GradualRolloutSettings
    {
        /// <summary>0–100. Users in the first N% of the hash space get the feature.</summary>
        public int Percentage { get; set; } = 0;

        /// <summary>Optional salt — lets two flags with same % have different user cohorts.</summary>
        public string? Salt { get; set; }
    }
}