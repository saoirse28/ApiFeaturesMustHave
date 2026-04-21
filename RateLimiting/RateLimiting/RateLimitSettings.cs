namespace RateLimiting.RateLimiting;

/// <summary>
/// Strongly-typed options for all rate limit policies.
/// Loaded from appsettings.json — allows threshold adjustments
/// per environment without code changes or redeployment.
/// </summary>
public sealed class RateLimitSettings
{
    public PerClientOptions PerClient { get; init; } = new();
    public PerClientOptions TrustedPartner { get; init; } = new();
    public PerClientOptions PremiumTier { get; init; } = new();
    public AuthOptions Authentication { get; init; } = new();
    public SensitiveOptions SensitiveOperation { get; init; } = new();
    public SearchOptions Search { get; init; } = new();
    public WebhookOptions Webhook { get; init; } = new();
    public AnonymousOptions AnonymousPublic { get; init; } = new();
}

public sealed class PerClientOptions
{
    public int PermitLimit { get; init; } = 100;
    public int WindowSeconds { get; init; } = 60;
    public int SegmentsPerWindow { get; init; } = 4;     // sliding window granularity
    public int QueueLimit { get; init; } = 0;     // 0 = no queuing, reject immediately
}

public sealed class AuthOptions
{
    public int PermitLimit { get; init; } = 5;
    public int WindowMinutes { get; init; } = 15;
    public int QueueLimit { get; init; } = 0;
}

public sealed class SensitiveOptions
{
    public int PermitLimit { get; init; } = 3;
    public int WindowHours { get; init; } = 1;
}

public sealed class SearchOptions
{
    public int TokenLimit { get; init; } = 30;
    public int TokensPerPeriodSeconds { get; init; } = 60;
    public int TokensPerPeriod { get; init; } = 30;
    public int QueueLimit { get; init; } = 5;
}

public sealed class WebhookOptions
{
    public int PermitLimit { get; init; } = 10;    // max concurrent webhook calls
    public int QueueLimit { get; init; } = 20;    // queue up to 20 waiting
}

public sealed class AnonymousOptions
{
    public int PermitLimit { get; init; } = 20;
    public int WindowSeconds { get; init; } = 60;
    public int SegmentsPerWindow { get; init; } = 4;
}