namespace ProperLogging.Logging;

/// <summary>
/// Compile-time source-generated log methods for the Order domain.
///
/// Why [LoggerMessage] over _logger.LogInformation(...)?
///   - Zero heap allocations — no boxing of value types, no string formatting
///     until the log level is actually enabled.
///   - Compile-time validation of message template placeholders.
///   - Structured properties are strongly typed — no magic strings at call sites.
///   - ~10x faster than _logger.LogXxx(...) in hot paths.
///
/// One partial class per domain area. Keep these focused — OrderLogs only
/// contains log definitions that belong to the Order bounded context.
/// </summary>
public static partial class OrderLogs
{
    // ── Informational — normal business events ────────────────────────────────

    [LoggerMessage(
        EventId = 1001,
        EventName = "OrderCreated",
        Level = LogLevel.Information,
        Message = "Order {OrderId} created for customer {CustomerId} — total {OrderTotal:C}")]
    public static partial void OrderCreated(
        this ILogger logger,
        string orderId,
        string customerId,
        decimal orderTotal);

    [LoggerMessage(
        EventId = 1002,
        EventName = "OrderStatusChanged",
        Level = LogLevel.Information,
        Message = "Order {OrderId} status changed from {OldStatus} to {NewStatus}")]
    public static partial void OrderStatusChanged(
        this ILogger logger,
        string orderId,
        string oldStatus,
        string newStatus);

    [LoggerMessage(
        EventId = 1003,
        EventName = "OrderShipped",
        Level = LogLevel.Information,
        Message = "Order {OrderId} shipped via {Carrier} — tracking {TrackingNumber}")]
    public static partial void OrderShipped(
        this ILogger logger,
        string orderId,
        string carrier,
        string trackingNumber);

    // ── Warning — recoverable anomalies ──────────────────────────────────────

    [LoggerMessage(
        EventId = 1010,
        EventName = "OrderItemOutOfStock",
        Level = LogLevel.Warning,
        Message = "Product {ProductId} is out of stock for order {OrderId} — quantity requested: {Quantity}")]
    public static partial void OrderItemOutOfStock(
        this ILogger logger,
        string productId,
        string orderId,
        int quantity);

    [LoggerMessage(
        EventId = 1011,
        EventName = "OrderPaymentRetried",
        Level = LogLevel.Warning,
        Message = "Payment retry {Attempt}/{MaxAttempts} for order {OrderId} — reason: {Reason}")]
    public static partial void OrderPaymentRetried(
        this ILogger logger,
        string orderId,
        int attempt,
        int maxAttempts,
        string reason);

    // ── Error — failures requiring investigation ──────────────────────────────

    [LoggerMessage(
        EventId = 1020,
        EventName = "OrderPaymentFailed",
        Level = LogLevel.Error,
        Message = "Payment failed for order {OrderId} — provider: {Provider}, code: {ErrorCode}")]
    public static partial void OrderPaymentFailed(
        this ILogger logger,
        string orderId,
        string provider,
        string errorCode,
        Exception exception);

    [LoggerMessage(
        EventId = 1021,
        EventName = "OrderFulfillmentFailed",
        Level = LogLevel.Error,
        Message = "Fulfillment failed for order {OrderId} after {ElapsedMs}ms")]
    public static partial void OrderFulfillmentFailed(
        this ILogger logger,
        string orderId,
        long elapsedMs,
        Exception exception);

    // ── Debug — dev/diagnostic noise, silent in production ───────────────────

    [LoggerMessage(
        EventId = 1030,
        EventName = "OrderCacheChecked",
        Level = LogLevel.Debug,
        Message = "Cache {CacheResult} for order {OrderId}")]
    public static partial void OrderCacheChecked(
        this ILogger logger,
        string cacheResult,
        string orderId);
}