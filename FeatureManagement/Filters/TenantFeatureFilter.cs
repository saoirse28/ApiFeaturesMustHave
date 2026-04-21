using Microsoft.FeatureManagement;

namespace FeatureManagement.Filters;

/// <summary>
/// Custom feature filter that activates a flag for specific tenant IDs.
///
/// Usage in appsettings.json:
/// "NewCheckoutFlow": {
///   "EnabledFor": [{
///     "Name": "Tenant",
///     "Parameters": { "AllowedTenants": ["tenant-acme", "tenant-globex"] }
///   }]
/// }
/// </summary>
[FilterAlias("Tenant")]
public sealed class TenantFeatureFilter : IFeatureFilter
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TenantFeatureFilter(IHttpContextAccessor httpContextAccessor)
        => _httpContextAccessor = httpContextAccessor;

    public Task<bool> EvaluateAsync(FeatureFilterEvaluationContext context)
    {
        var settings = context.Parameters.Get<TenantFilterSettings>()
            ?? new TenantFilterSettings();

        if (settings.AllowedTenants is null || settings.AllowedTenants.Length == 0)
            return Task.FromResult(false);

        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is null)
            return Task.FromResult(false);

        // Read tenant ID from JWT claim or request header
        var tenantId = httpContext.User.FindFirst("tenant_id")?.Value
            ?? httpContext.Request.Headers["X-Tenant-Id"].FirstOrDefault();

        if (string.IsNullOrEmpty(tenantId))
            return Task.FromResult(false);

        var isAllowed = settings.AllowedTenants
            .Any(t => string.Equals(t, tenantId, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(isAllowed);
    }

    private sealed class TenantFilterSettings
    {
        public string[]? AllowedTenants { get; set; }
    }
}