namespace ServerSentEvents.SSE;

/// <summary>
/// All event type name constants used across server and client.
/// Client subscribes via: source.addEventListener(SseEventTypes.OrderShipped, handler)
/// </summary>
public static class SseEventTypes
{
    public const string Connected = "connected";
    public const string Heartbeat = "heartbeat";
    public const string Error = "error";

    public const string OrderCreated = "order.created";
    public const string OrderUpdated = "order.updated";
    public const string OrderShipped = "order.shipped";
    public const string OrderDelivered = "order.delivered";
    public const string OrderCancelled = "order.cancelled";

    public const string PaymentReceived = "payment.received";
    public const string PaymentFailed = "payment.failed";

    public const string Notification = "notification";
    public const string MetricUpdate = "metric.update";

    public const string Message = "metric.update";
}