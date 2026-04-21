using Microsoft.FeatureManagement;
using System.Security.Claims;

namespace FeatureManagement.Filters;

/// <summary>
/// Custom filter that activates a flag for users in specific roles.
/// Useful for internal beta testers and admin previews.
///
/// Usage in appsettings.json:
/// "BetaDashboard": {
///   "EnabledFor": [{
///     "Name": "UserRole",
///     "Parameters": { "Roles": ["BetaTester", "Admin"] }
///   }]
/// }
/// </summary>
[FilterAlias("UserRole")]
public sealed class UserRoleFeatureFilter : IFeatureFilter
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<UserRoleFeatureFilter> _logger;

    public UserRoleFeatureFilter(
        IHttpContextAccessor httpContextAccessor,
        ILogger<UserRoleFeatureFilter> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger              = logger;
    }

    public Task<bool> EvaluateAsync(FeatureFilterEvaluationContext context)
    {
        var settings = context.Parameters.Get<RoleFilterSettings>()
            ?? new RoleFilterSettings();

        if (settings.Roles is null || settings.Roles.Length == 0)
        {
            _logger.LogWarning(
                "UserRoleFeatureFilter for '{Feature}' has no roles configured",
                context.FeatureName);
            return Task.FromResult(false);
        }

        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.User is null)
            return Task.FromResult(false);

        var userRoles = httpContext.User
            .FindAll(ClaimTypes.Role)
            .Select(c => c.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var isEnabled = settings.Roles.Any(role => userRoles.Contains(role));
        return Task.FromResult(isEnabled);
    }

    private sealed class RoleFilterSettings
    {
        public string[]? Roles { get; set; }
    }
}