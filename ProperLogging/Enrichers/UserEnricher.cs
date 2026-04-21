using ProperLogging.Logging;
using Serilog.Core;
using Serilog.Events;
using System.Security.Claims;

namespace ProperLogging.Enrichers;

/// <summary>
/// Adds authenticated user information to every log event.
/// Reads from IHttpContextAccessor so it works inside any service
/// that processes an HTTP request — not just controllers.
/// </summary>
public sealed class UserEnricher : ILogEventEnricher
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserEnricher(IHttpContextAccessor httpContextAccessor)
        => _httpContextAccessor = httpContextAccessor;

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var context = _httpContextAccessor.HttpContext;
        if (context?.User?.Identity?.IsAuthenticated != true)
            return;

        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? context.User.FindFirstValue("sub");

        var userEmail = context.User.FindFirstValue(ClaimTypes.Email)
                     ?? context.User.FindFirstValue("email");

        var tenantId = context.User.FindFirstValue("tenant_id");

        if (userId is not null)
            logEvent.AddPropertyIfAbsent(
                propertyFactory.CreateProperty(LoggingConstants.UserId, userId));

        if (userEmail is not null)
            logEvent.AddPropertyIfAbsent(
                propertyFactory.CreateProperty(LoggingConstants.UserEmail, userEmail));

        if (tenantId is not null)
            logEvent.AddPropertyIfAbsent(
                propertyFactory.CreateProperty(LoggingConstants.TenantId, tenantId));
    }
}
