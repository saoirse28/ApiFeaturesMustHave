namespace ProperLogging.Logging;

/// <summary>
/// Source-generated log definitions for the Payment bounded context.
/// Separate file per domain — keeps log definitions cohesive and searchable.
/// </summary>
public static partial class PaymentLogs
{
    [LoggerMessage(
        EventId = 2001,
        EventName = "PaymentInitiated",
        Level = LogLevel.Information,
        Message = "Payment initiated for order {OrderId} — amount {Amount:C}, method {PaymentMethod}")]
    public static partial void PaymentInitiated(
        this ILogger logger,
        string orderId,
        decimal amount,
        string paymentMethod);

    [LoggerMessage(
        EventId = 2002,
        EventName = "PaymentSucceeded",
        Level = LogLevel.Information,
        Message = "Payment succeeded for order {OrderId} — transaction {TransactionId}")]
    public static partial void PaymentSucceeded(
        this ILogger logger,
        string orderId,
        string transactionId);

    [LoggerMessage(
        EventId = 2003,
        EventName = "PaymentGatewayTimeout",
        Level = LogLevel.Warning,
        Message = "Payment gateway timeout for order {OrderId} after {ElapsedMs}ms — gateway: {Gateway}")]
    public static partial void PaymentGatewayTimeout(
        this ILogger logger,
        string orderId,
        long elapsedMs,
        string gateway);

    [LoggerMessage(
        EventId = 2004,
        EventName = "PaymentFraudFlagged",
        Level = LogLevel.Warning,
        Message = "Payment for order {OrderId} flagged by fraud engine — score: {FraudScore}")]
    public static partial void PaymentFraudFlagged(
        this ILogger logger,
        string orderId,
        double fraudScore);
}