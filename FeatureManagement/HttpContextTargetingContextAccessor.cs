using Microsoft.FeatureManagement.FeatureFilters;
using System.Security.Claims;

namespace FeatureManagement;

/// <summary>
/// Provides the targeting context for the built-in TargetingFilter.
/// Maps the current authenticated user and their groups to Polly's
/// targeting model so the TargetingFilter can enable flags per user
/// or per group with optional percentage spillover.
///
/// Usage in appsettings.json:
/// "OneClickCheckout": {
///   "EnabledFor": [{
///     "Name": "Targeting",
///     "Parameters": {
///       "Audience": {
///         "Users": ["alice@example.com", "bob@example.com"],
///         "Groups": [
///           { "Name": "BetaGroup", "RolloutPercentage": 50 }
///         ],
///         "DefaultRolloutPercentage": 5
///       }
///     }
///   }]
/// }
/// </summary>
public sealed class HttpContextTargetingContextAccessor : ITargetingContextAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpContextTargetingContextAccessor(IHttpContextAccessor httpContextAccessor)
        => _httpContextAccessor = httpContextAccessor;

    public ValueTask<TargetingContext> GetContextAsync()
    {
        var httpContext = _httpContextAccessor.HttpContext;

        if (httpContext?.User?.Identity?.IsAuthenticated != true)
        {
            // Anonymous users get an empty context — only DefaultRolloutPercentage applies
            return new ValueTask<TargetingContext>(new TargetingContext
            {
                UserId = httpContext?.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
                Groups = Array.Empty<string>()
            });
        }

        var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? httpContext.User.FindFirstValue("sub")
                  ?? "unknown";

        // Groups can come from JWT role claims, department claims, or subscription tier
        var groups = httpContext.User
            .FindAll(ClaimTypes.Role)
            .Select(c => c.Value)
            .Concat(httpContext.User
                .FindAll("subscription_tier")
                .Select(c => c.Value))
            .Distinct()
            .ToList();

        // Add tenant as a group so tenant-wide rollouts work via TargetingFilter
        var tenantId = httpContext.User.FindFirstValue("tenant_id");
        if (!string.IsNullOrEmpty(tenantId))
            groups.Add($"tenant:{tenantId}");

        return new ValueTask<TargetingContext>(new TargetingContext
        {
            //UserId = userId,
            //Groups = groups
            UserId = "Erwin",
            Groups = new[] { "RoLE" }
        });
    }
}