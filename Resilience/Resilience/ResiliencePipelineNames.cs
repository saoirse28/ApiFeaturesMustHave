namespace Resilience.Resilience;

/// <summary>
/// Centralizes all resilience pipeline name strings.
/// Prevents typos at call sites — a wrong pipeline name silently
/// falls back to no resilience, which is worse than an exception.
/// </summary>
public static class ResiliencePipelineNames
{
    // ── HttpClient pipelines (used with AddHttpClient) ────────────────────────
    public const string Payment = "payment-pipeline";
    public const string Inventory = "inventory-pipeline";
    public const string Notification = "notification-pipeline";

    // ── Non-HTTP pipelines (used with ResiliencePipelineProvider<string>) ─────
    public const string Database = "database-pipeline";
    public const string Redis = "redis-pipeline";
    public const string MessageBus = "messagebus-pipeline";
}