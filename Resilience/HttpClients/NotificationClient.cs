using System.Net.Http.Json;

namespace Resilience.HttpClients;

/// <summary>
/// Typed HTTP client for the Notification service.
/// Uses a hedging strategy — sends a second request after a short delay
/// if the first hasn't responded. Takes whichever responds first.
/// Ideal for non-critical, latency-sensitive operations.
/// </summary>
public sealed class NotificationClient
{
    private readonly HttpClient _http;
    private readonly ILogger<NotificationClient> _logger;

    public NotificationClient(HttpClient http, ILogger<NotificationClient> logger)
    {
        _http   = http;
        _logger = logger;
    }

    public async Task<bool> SendOrderConfirmationAsync(
        string orderId,
        string userId,
        CancellationToken ct = default)
    {
        try
        {
            var response = await _http.PostAsJsonAsync(
                "/api/notifications/order-confirmation",
                new { orderId, userId },
                ct);

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            // Notifications are fire-and-forget — degrade silently
            _logger.LogWarning(ex,
                "Failed to send order confirmation for {OrderId} — continuing",
                orderId);
            return false;
        }
    }

    internal void SendOrderConfirmationAsync(string id, object userId)
    {
        throw new NotImplementedException();
    }
}