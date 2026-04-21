using RateLimiting.DTOs;
using System.Collections.Concurrent;
using System.Threading.RateLimiting;

namespace RateLimiting.Services;

/// <summary>
/// Demonstrates applying rate limiting programmatically inside a service —
/// useful for background jobs, message consumers, or outbound API throttling
/// where middleware does not apply.
///
/// Example: throttle outbound webhook deliveries so we don't hammer
/// a customer's endpoint more than 100 times per minute.
/// </summary>
public sealed class WebhookDeliveryService : IAsyncDisposable
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<WebhookDeliveryService> _logger;

    // Per-endpoint throttle — each webhook destination gets its own limiter
    private readonly ConcurrentDictionary<string, RateLimiter> _limiters = new();

    public WebhookDeliveryService(
        IHttpClientFactory httpClientFactory,
        ILogger<WebhookDeliveryService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger            = logger;
    }

    public async Task DeliverAsync(
        WebhookEvent evt,
        string destinationUrl,
        CancellationToken ct = default)
    {
        RateLimiter limiter = _limiters.GetOrAdd(
            destinationUrl,
            _ => new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions
            {
                PermitLimit       = 100,
                Window            = TimeSpan.FromMinutes(1),
                SegmentsPerWindow = 4,
                QueueLimit        = 500,   // queue overflow events — don't drop them
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                AutoReplenishment = true
            }));

        // Acquire a lease — waits if the queue has capacity, rejects if full
        using RateLimitLease lease = await limiter.AcquireAsync(permitCount: 1, ct);

        if (!lease.IsAcquired)
        {
            _logger.LogWarning(
                "Webhook delivery queue full for {Url} — dropping event {EventId}",
                destinationUrl, evt.Id);
            return;
        }

        var http = _httpClientFactory.CreateClient("WebhookDelivery");

        try
        {
            var response = await http.PostAsJsonAsync(destinationUrl, evt, ct);
            response.EnsureSuccessStatusCode();

            _logger.LogInformation(
                "Webhook {EventId} delivered to {Url} — status {Status}",
                evt.Id, destinationUrl, (int)response.StatusCode);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex,
                "Webhook {EventId} delivery failed to {Url}",
                evt.Id, destinationUrl);
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var limiter in _limiters.Values)
            await limiter.DisposeAsync();
        _limiters.Clear();
    }
}