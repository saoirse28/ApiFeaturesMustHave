namespace Resilience.Resilience;

/// <summary>
/// Strongly-typed configuration models for resilience settings.
/// Loaded from appsettings.json — allows ops teams to tune retry
/// counts and circuit breaker thresholds without code changes or redeployment.
/// </summary>
public sealed class ResilienceSettings
{
    public HttpClientResilienceOptions Payment { get; init; } = new();
    public HttpClientResilienceOptions Inventory { get; init; } = new();
    public NonHttpResilienceOptions Database { get; init; } = new();
    public NonHttpResilienceOptions Redis { get; init; } = new();
}

public sealed class HttpClientResilienceOptions
{
    public int MaxRetryAttempts { get; init; } = 3;
    public double RetryDelayMs { get; init; } = 300;
    public double CircuitBreakerFailureRatio { get; init; } = 0.5;
    public int CircuitBreakerSamplingSeconds { get; init; } = 30;
    public int CircuitBreakerMinThroughput { get; init; } = 10;
    public int CircuitBreakerBreakSeconds { get; init; } = 30;
    public int TotalTimeoutSeconds { get; init; } = 10;
    public int AttemptTimeoutSeconds { get; init; } = 3;
}

public sealed class NonHttpResilienceOptions
{
    public int MaxRetryAttempts { get; init; } = 2;
    public int RetryDelayMs { get; init; } = 200;
    public int TimeoutSeconds { get; init; } = 5;
    public double FailureRatio { get; init; } = 0.6;
    public int SamplingSeconds { get; init; } = 20;
}