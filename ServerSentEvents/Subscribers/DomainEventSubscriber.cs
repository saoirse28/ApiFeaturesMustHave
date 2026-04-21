using MediatR;
using ServerSentEvents.Publishers;
using ServerSentEvents.SSE;

namespace ServerSentEvents.Subscribers;

/// <summary>
/// Bridges MediatR domain event notifications to the SSE publisher.
/// Listens for domain events published via MediatR and forwards
/// them to the appropriate SSE channels.
///
/// This keeps the SSE infrastructure completely decoupled from business logic —
/// OrderService publishes domain events via MediatR without knowing SSE exists.
/// </summary>

// ── Order domain event notification handlers ──────────────────────────────────

public sealed record OrderCreatedEvent(
    string OrderId, string UserId, decimal Total) : INotification;

public sealed record OrderShippedEvent(
    string OrderId, string UserId, string TrackingNumber, string Carrier) : INotification;

public sealed record PaymentReceivedEvent(
    string OrderId, string UserId, decimal Amount, string TransactionId) : INotification;

public sealed record PaymentFailedEvent(
    string OrderId, string UserId, string Reason) : INotification;


public sealed class OrderCreatedEventHandler(IEventPublisher publisher) : INotificationHandler<OrderCreatedEvent>
{
    private readonly IEventPublisher _publisher = publisher;

    public Task Handle(OrderCreatedEvent notification, CancellationToken ct)
        => _publisher.PublishToUserAsync(
            notification.UserId,
            SseEventTypes.OrderCreated,
            new
            {
                orderId = notification.OrderId,
                total = notification.Total,
                timestamp = DateTimeOffset.UtcNow
            },
            ct);
}

public sealed class OrderShippedEventHandler : INotificationHandler<OrderShippedEvent>
{
    private readonly IEventPublisher _publisher;

    public OrderShippedEventHandler(IEventPublisher publisher)
        => _publisher = publisher;

    public Task Handle(OrderShippedEvent notification, CancellationToken ct)
        => _publisher.PublishToUserAsync(
            notification.UserId,
            SseEventTypes.OrderShipped,
            new
            {
                orderId = notification.OrderId,
                trackingNumber = notification.TrackingNumber,
                carrier = notification.Carrier,
                timestamp = DateTimeOffset.UtcNow
            },
            ct);
}

public sealed class PaymentReceivedEventHandler : INotificationHandler<PaymentReceivedEvent>
{
    private readonly IEventPublisher _publisher;

    public PaymentReceivedEventHandler(IEventPublisher publisher)
        => _publisher = publisher;

    public Task Handle(PaymentReceivedEvent notification, CancellationToken ct)
        => _publisher.PublishToUserAsync(
            notification.UserId,
            SseEventTypes.PaymentReceived,
            new
            {
                orderId = notification.OrderId,
                amount = notification.Amount,
                transactionId = notification.TransactionId,
                timestamp = DateTimeOffset.UtcNow
            },
            ct);
}


public sealed class PaymentFailedEventHandler : INotificationHandler<PaymentFailedEvent>
{
    private readonly IEventPublisher _publisher;

    public PaymentFailedEventHandler(IEventPublisher publisher)
        => _publisher = publisher;

    public Task Handle(PaymentFailedEvent notification, CancellationToken ct)
        => _publisher.PublishToUserAsync(
            notification.UserId,
            SseEventTypes.PaymentFailed,
            new
            {
                orderId = notification.OrderId,
                reason = notification.Reason,
                timestamp = DateTimeOffset.UtcNow
            },
            ct);
}