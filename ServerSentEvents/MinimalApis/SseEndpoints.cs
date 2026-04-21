using ServerSentEvents.Channels;
using ServerSentEvents.Services;
using ServerSentEvents.SSE;
using System.Net.ServerSentEvents;
using System.Runtime.CompilerServices;
using System.Security.Claims;

namespace ServerSentEvents.MinimalApis;

public static class SseEndpoints
{
    public static IEndpointRouteBuilder MapSseEndpoints(
        this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/sse")
            .RequireAuthorization()
            .WithTags("SSE");

        // ── 1. Personal stream ────────────────────────────────────────────────
        group.MapGet("/stream", (
            HttpContext context,
            EventChannelRegistry registry,
            ILogger<EventChannelRegistry> logger,
            CancellationToken ct) =>
        {
            var userId = GetUserId(context);
            var connectionId = NewConnectionId();
            var channel = registry.Register(userId, connectionId);

            logger.LogInformation(
                "SSE stream opened — user={UserId} conn={ConnectionId}",
                userId, connectionId);

            return TypedResults.ServerSentEvents(
                PersonalEvents(userId, connectionId, channel, registry, logger, ct));
        })
        .WithName("StreamPersonal")
        .WithSummary("Personal real-time event stream");

        // ── 2. Order tracking stream ──────────────────────────────────────────
        group.MapGet("/orders/{orderId}/track", (
            string orderId,
            HttpContext context,
            EventChannelRegistry registry,
            IOrderService orders,
            ILogger<EventChannelRegistry> logger,
            CancellationToken ct) =>
        {
            var userId = GetUserId(context);
            var connectionId = NewConnectionId($"order-{orderId}");
            var channel = registry.Register(userId, connectionId, capacity: 20);

            return TypedResults.ServerSentEvents(
                OrderTrackingEvents(orderId, userId, connectionId, channel, orders, registry, logger, ct));
        })
        .WithName("TrackOrder")
        .WithSummary("Live order status updates");

        // ── 3. Notification stream ────────────────────────────────────────────
        group.MapGet("/notifications", (
            HttpContext context,
            EventChannelRegistry registry,
            CancellationToken ct) =>
        {
            var userId = GetUserId(context);
            var connectionId = NewConnectionId("notif");
            var channel = registry.Register(userId, connectionId, capacity: 50);

            return TypedResults.ServerSentEvents(
                NotificationEvents(userId, connectionId, channel, registry, ct),
                eventType: SseEventTypes.Notification);
        })
        .WithName("StreamNotifications")
        .WithSummary("Real-time notification stream");

        // ── 4. Dashboard metrics (admin only) ─────────────────────────────────
        group.MapGet("/dashboard", (
            IDashboardService dashboard,
            CancellationToken ct) =>
            TypedResults.ServerSentEvents(
                DashboardEvents(dashboard, ct),
                eventType: SseEventTypes.MetricUpdate))
        .WithName("StreamDashboard")
        .WithSummary("Live dashboard metrics — pushed every 5 seconds")
        .RequireAuthorization("AdminOnly");

        return app;
    }

    // ── Async iterators ───────────────────────────────────────────────────────

    private static async IAsyncEnumerable<SseItem<string>> PersonalEvents(
        string userId,
        string connectionId,
        BoundedEventChannel channel,
        EventChannelRegistry registry,
        ILogger logger,
        [EnumeratorCancellation] CancellationToken ct)
    {
        yield return SseEvent.Connected(connectionId).ToSseItem();

        try
        {
            await foreach (var evt in channel.ReadAllAsync(ct))
                yield return evt.ToSseItem();
        }
        finally
        {
            registry.Unregister(userId, connectionId);
            logger.LogInformation(
                "SSE stream closed — user={UserId} conn={ConnectionId}",
                userId, connectionId);
        }
    }

    private static async IAsyncEnumerable<SseItem<string>> OrderTrackingEvents(
        string orderId,
        string userId,
        string connectionId,
        BoundedEventChannel channel,
        IOrderService orders,
        EventChannelRegistry registry,
        ILogger<EventChannelRegistry> logger,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var current = await orders.GetByIdAsync(orderId, ct);

        if (current is null)
        {
            yield return SseEvent
                .Create(SseEventTypes.Error, new { message = "Order not found", orderId })
                .ToSseItem();
            yield break;
        }

        yield return SseEvent.Create(SseEventTypes.OrderUpdated, current).ToSseItem();

        try
        {
            await foreach (var evt in channel.ReadAllAsync(ct))
            {
                yield return evt.ToSseItem();

                if (evt.EventType is SseEventTypes.OrderDelivered
                                  or SseEventTypes.OrderCancelled)
                {
                    logger.LogInformation(
                        "Order {OrderId} terminal — closing SSE stream", orderId);
                    yield break;
                }
            }
        }
        finally
        {
            registry.Unregister(userId, connectionId);
        }
    }

    private static async IAsyncEnumerable<SseItem<string>> NotificationEvents(
        string userId,
        string connectionId,
        BoundedEventChannel channel,
        EventChannelRegistry registry,
        [EnumeratorCancellation] CancellationToken ct)
    {
        try
        {
            await foreach (var evt in channel.ReadAllAsync(ct))
                yield return evt.ToSseItem();
        }
        finally
        {
            registry.Unregister(userId, connectionId);
        }
    }

    private static async IAsyncEnumerable<SseItem<string>> DashboardEvents(
        IDashboardService dashboard,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var snapshot = await dashboard.GetSnapshotAsync(ct);
        yield return SseEvent.Create(SseEventTypes.MetricUpdate, snapshot).ToSseItem();

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));

        while (await timer.WaitForNextTickAsync(ct))
        {
            var metrics = await dashboard.GetLiveMetricsAsync(ct);
            yield return SseEvent.Create(SseEventTypes.MetricUpdate, metrics).ToSseItem();
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string GetUserId(HttpContext context) =>
        context.User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? context.User.FindFirstValue("sub")
        ?? "anonymous";

    private static string NewConnectionId(string? prefix = null) =>
        prefix is null
            ? Guid.NewGuid().ToString("N")
            : $"{prefix}-{Guid.NewGuid():N}";
}