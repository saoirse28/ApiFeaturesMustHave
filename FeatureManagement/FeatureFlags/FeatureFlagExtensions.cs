using FeatureManagement.Filters;
using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.FeatureFilters;

namespace FeatureManagement.FeatureFlags;

public static class FeatureFlagExtensions
{
    /// <summary>
    /// Registers the feature management system with all built-in
    /// and custom filters. Called once from Program.cs.
    /// </summary>
    public static IServiceCollection AddFeatureManagements(
        this IServiceCollection services)
    {
        services
            .AddFeatureManagement()

            // ── Built-in Microsoft filters ───────────────────────────────────

            // Percentage rollout — randomly enabled for X% of requests
            .AddFeatureFilter<PercentageFilter>()

            // Time-window activation — flag is on only between two datetimes
            .AddFeatureFilter<TimeWindowFilter>()

            // Targeting — enable for specific users, groups, or percentage
            .AddFeatureFilter<TargetingFilter>()

            // ── Custom filters ───────────────────────────────────────────────

            // Per-tenant activation — flag on/off per tenant ID
            .AddFeatureFilter<TenantFeatureFilter>()

            // Role-based activation — flag on for specific JWT roles
            .AddFeatureFilter<UserRoleFeatureFilter>()

            // Gradual rollout — deterministic % based on user ID hash
            // (same user always gets same experience — no flickering)
            .AddFeatureFilter<GradualRolloutFilter>()

            // Environment-based — flag on only in specific environments
            .AddFeatureFilter<EnvironmentFeatureFilter>();

        // Register the targeting context accessor for TargetingFilter
        services.AddSingleton<ITargetingContextAccessor, HttpContextTargetingContextAccessor>();

        return services;
    }
}