using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.FeatureManagement.Mvc;

namespace FeatureManagement.FeatureFlags;

/// <summary>
/// Controls what happens when a request hits an endpoint guarded by
/// [FeatureGate] and the flag is disabled.
///
/// Default behavior: returns HTTP 404 (endpoint appears to not exist).
///
/// Override to return 503 during maintenance, or redirect to
/// a "coming soon" page for features in preview.
/// </summary>
public sealed class FeatureNotEnabledHandler : IDisabledFeaturesHandler
{
    public Task HandleDisabledFeatures(
        IEnumerable<string> features,
        ActionExecutingContext context)
    {
        var featureList = features.ToList();

        // Kill switch: maintenance mode → 503
        if (featureList.Contains(FeatureFlags.MaintenanceMode))
        {
            context.Result = new ObjectResult(new
            {
                type = "https://api.myapp.com/errors/maintenance",
                title = "Service Unavailable",
                status = 503,
                detail = "The service is under maintenance. Please try again later."
            })
            { StatusCode = StatusCodes.Status503ServiceUnavailable };

            return Task.CompletedTask;
        }

        // Feature preview: return 404 with a hint
        context.Result = new ObjectResult(new
        {
            type = "https://api.myapp.com/errors/feature-not-available",
            title = "Feature Not Available",
            status = 404,
            detail = "This feature is not available for your account.",
            features = featureList
        })
        { StatusCode = StatusCodes.Status404NotFound };

        return Task.CompletedTask;
    }
}