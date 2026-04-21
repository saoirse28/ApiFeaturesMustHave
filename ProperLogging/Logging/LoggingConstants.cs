namespace ProperLogging.Logging;

/// <summary>
/// Centralizes all structured log property names.
/// Prevents typos in property keys — a typo means a missing field
/// in Seq/Splunk/Datadog that is invisible until you search for it.
/// </summary>
public static class LoggingConstants
{
    // ── Request context ───────────────────────────────────────────────────────
    public const string CorrelationId = "CorrelationId";
    public const string RequestId = "RequestId";
    public const string ClientIp = "ClientIp";
    public const string UserAgent = "UserAgent";
    public const string RequestPath = "RequestPath";
    public const string RequestMethod = "RequestMethod";
    public const string StatusCode = "StatusCode";
    public const string ElapsedMs = "ElapsedMs";

    // ── Identity ──────────────────────────────────────────────────────────────
    public const string UserId = "UserId";
    public const string UserEmail = "UserEmail";
    public const string TenantId = "TenantId";
    public const string Roles = "Roles";

    // ── Domain ────────────────────────────────────────────────────────────────
    public const string OrderId = "OrderId";
    public const string CustomerId = "CustomerId";
    public const string ProductId = "ProductId";
    public const string OrderTotal = "OrderTotal";
    public const string PaymentMethod = "PaymentMethod";

    // ── Infrastructure ────────────────────────────────────────────────────────
    public const string MachineName = "MachineName";
    public const string Environment = "Environment";
    public const string AppVersion = "AppVersion";
    public const string TraceId = "TraceId";
    public const string SpanId = "SpanId";

    // ── Event IDs — unique per log-call site ──────────────────────────────────
    public static class EventIds
    {
        public static readonly EventId OrderCreated = new(1001, "OrderCreated");
        public static readonly EventId OrderUpdated = new(1002, "OrderUpdated");
        public static readonly EventId OrderDeleted = new(1003, "OrderDeleted");
        public static readonly EventId PaymentSucceeded = new(1010, "PaymentSucceeded");
        public static readonly EventId PaymentFailed = new(1011, "PaymentFailed");
        public static readonly EventId UserLoggedIn = new(2001, "UserLoggedIn");
        public static readonly EventId UserLoggedOut = new(2002, "UserLoggedOut");
        public static readonly EventId CacheMiss = new(3001, "CacheMiss");
        public static readonly EventId CacheHit = new(3002, "CacheHit");
        public static readonly EventId DbQuerySlow = new(4001, "DbQuerySlow");
        public static readonly EventId ExternalCallFail = new(5001, "ExternalCallFailed");
    }
}