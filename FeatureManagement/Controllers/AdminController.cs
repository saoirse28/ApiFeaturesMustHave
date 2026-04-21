using FeatureManagement.FeatureFlags;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement;

namespace FeatureManagement.Controllers;

/// <summary>
/// Admin endpoints for runtime flag inspection and management.
/// Allows QA and ops teams to inspect current flag state per user
/// without redeploying configuration.
///
/// IMPORTANT: Protect these endpoints — exposing flag state leaks
/// your feature roadmap and rollout strategy.
/// </summary>
[ApiController]
[Route("api/admin/flags")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly IFeatureManager _features;
    private readonly IConfiguration _configuration;

    public AdminController(
        IFeatureManager features,
        IConfiguration configuration)
    {
        _features      = features;
        _configuration = configuration;
    }

    /// <summary>
    /// Returns the current enabled/disabled state of all known flags.
    /// Useful for debugging flag state in each environment.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAllFlags(CancellationToken ct)
    {
        var allFlags = typeof(FeatureFlags.FeatureFlags)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(f => f.FieldType == typeof(string))
            .Select(f => (string)f.GetValue(null)!)
            .ToList();

        var states = new Dictionary<string, object>();

        foreach (var flag in allFlags)
        {
            var isEnabled = await _features.IsEnabledAsync(flag);
            states[flag] = new
            {
                enabled = isEnabled,
                // Include raw config so ops can see which filters are configured
                configuration = _configuration
                    .GetSection($"FeatureManagement:{flag}")
                    .Get<object>()
            };
        }

        return Ok(new
        {
            timestamp = DateTimeOffset.UtcNow,
            environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
            flags = states
        });
    }

    /// <summary>
    /// Checks whether a specific flag is enabled for the current request context.
    /// Useful for QA to verify their account is in the correct rollout cohort.
    /// </summary>
    [HttpGet("{flagName}")]
    public async Task<IActionResult> GetFlag(string flagName)
    {
        // Validate the flag name exists to prevent probing unknown flags
        var knownFlags = typeof(FeatureFlags.FeatureFlags)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(f => f.FieldType == typeof(string))
            .Select(f => (string)f.GetValue(null)!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (!knownFlags.Contains(flagName))
            return NotFound(new { error = $"Unknown feature flag: {flagName}" });

        var isEnabled = await _features.IsEnabledAsync(flagName);

        return Ok(new
        {
            flag = flagName,
            enabled = isEnabled,
            timestamp = DateTimeOffset.UtcNow,
            user = User.FindFirst("sub")?.Value ?? "anonymous"
        });
    }
}