using Microsoft.FeatureManagement;

namespace FeatureManagement.Filters;

/// <summary>
/// Custom filter that activates a flag only in specific environments.
/// Prevents infrastructure flags from leaking into production prematurely.
///
/// Usage in appsettings.json:
/// "HybridCacheEnabled": {
///   "EnabledFor": [{
///     "Name": "Environment",
///     "Parameters": { "Environments": ["Development", "Staging"] }
///   }]
/// }
/// </summary>
[FilterAlias("Environment")]
public sealed class EnvironmentFeatureFilter : IFeatureFilter
{
    private readonly IHostEnvironment _environment;

    public EnvironmentFeatureFilter(IHostEnvironment environment)
        => _environment = environment;

    public Task<bool> EvaluateAsync(FeatureFilterEvaluationContext context)
    {
        var settings = context.Parameters.Get<EnvironmentFilterSettings>()
            ?? new EnvironmentFilterSettings();

        if (settings.Environments is null || settings.Environments.Length == 0)
            return Task.FromResult(false);

        var isEnabled = settings.Environments.Any(env =>
            string.Equals(env, _environment.EnvironmentName,
                StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(isEnabled);
    }

    private sealed class EnvironmentFilterSettings
    {
        public string[]? Environments { get; set; }
    }
}