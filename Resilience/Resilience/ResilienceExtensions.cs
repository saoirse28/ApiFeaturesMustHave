using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;

namespace Resilience.Resilience;

public static class ResilienceExtensions
{
    /// <summary>
    /// Registers all resilience pipelines.
    /// Called once from Program.cs — keeps resilience wiring out of business code.
    /// </summary>
    public static IServiceCollection AddResiliencePipelines(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var settings = configuration
            .GetSection("Resilience")
            .Get<ResilienceSettings>() ?? new ResilienceSettings();

        // ── Non-HTTP pipelines ─────────────────────────────────────────────────
        AddDatabasePipeline(services, settings.Database);
        AddRedisPipeline(services, settings.Redis);
        AddMessageBusPipeline(services);

        return services;
    }

    // ── Database resilience pipeline ──────────────────────────────────────────
    private static void AddDatabasePipeline(
        IServiceCollection services,
        NonHttpResilienceOptions opts)
    {
        services.AddResiliencePipeline(ResiliencePipelineNames.Database, builder =>
        {
            builder
                // 1. Overall operation timeout — outer fence
                .AddTimeout(new TimeoutStrategyOptions
                {
                    Timeout = TimeSpan.FromSeconds(opts.TimeoutSeconds * 3),
                    OnTimeout = args =>
                    {
                        args.Context.Properties.TryGetValue(
                            new ResiliencePropertyKey<string>("OperationKey"),
                            out var opKey);
                        Console.WriteLine($"[DB] Total timeout exceeded for operation '{opKey}'");
                        return ValueTask.CompletedTask;
                    }
                })

                // 2. Circuit breaker — opens after failure threshold is crossed
                .AddCircuitBreaker(new CircuitBreakerStrategyOptions
                {
                    FailureRatio         = opts.FailureRatio,
                    SamplingDuration     = TimeSpan.FromSeconds(opts.SamplingSeconds),
                    MinimumThroughput    = 5,
                    BreakDuration        = TimeSpan.FromSeconds(30),
                    ShouldHandle         = new PredicateBuilder()
                        .Handle<TimeoutRejectedException>()
                        .Handle<InvalidOperationException>()
                        .Handle<SqlException>(),
                    OnOpened = args =>
                    {
                        Console.WriteLine(
                            $"[DB] Circuit OPENED — break for {args.BreakDuration.TotalSeconds}s. " +
                            $"Outcome: {args.Outcome.Exception?.Message}");
                        return ValueTask.CompletedTask;
                    },
                    OnClosed = args =>
                    {
                        Console.WriteLine("[DB] Circuit CLOSED — DB is healthy again");
                        return ValueTask.CompletedTask;
                    },
                    OnHalfOpened = args =>
                    {
                        Console.WriteLine("[DB] Circuit HALF-OPEN — testing one request");
                        return ValueTask.CompletedTask;
                    }
                })

                .AddCircuitBreaker(new CircuitBreakerStrategyOptions
                {
                    // ... existing options ...

                    OnOpened = args =>
                    {
                        CircuitStateTracker.SetOpen(ResiliencePipelineNames.Database);
                        Console.WriteLine(
                            $"[DB] Circuit OPENED — break for {args.BreakDuration.TotalSeconds}s");
                        return ValueTask.CompletedTask;
                    },
                    OnClosed = args =>
                    {
                        CircuitStateTracker.SetClosed(ResiliencePipelineNames.Database);
                        Console.WriteLine("[DB] Circuit CLOSED — DB is healthy again");
                        return ValueTask.CompletedTask;
                    },
                    OnHalfOpened = args =>
                    {
                        CircuitStateTracker.SetHalfOpen(ResiliencePipelineNames.Database);
                        Console.WriteLine("[DB] Circuit HALF-OPEN — testing probe request");
                        return ValueTask.CompletedTask;
                    }
                })

                // 3. Retry — jittered exponential backoff
                .AddRetry(new RetryStrategyOptions
                {
                    MaxRetryAttempts  = opts.MaxRetryAttempts,
                    Delay             = TimeSpan.FromMilliseconds(opts.RetryDelayMs),
                    BackoffType       = DelayBackoffType.Exponential,
                    UseJitter         = true,    // ±25% jitter prevents retry storms
                    ShouldHandle      = new PredicateBuilder()
                        .Handle<TimeoutRejectedException>()
                        .Handle<SqlException>(ex =>
                            IsTransientSqlError(ex.Number)),
                    OnRetry = args =>
                    {
                        Console.WriteLine(
                            $"[DB] Retry {args.AttemptNumber + 1}/{opts.MaxRetryAttempts} " +
                            $"after {args.RetryDelay.TotalMilliseconds:0}ms — " +
                            $"{args.Outcome.Exception?.Message}");
                        return ValueTask.CompletedTask;
                    }
                })

                // 4. Per-attempt timeout — inner fence around each individual try
                .AddTimeout(new TimeoutStrategyOptions
                {
                    Timeout = TimeSpan.FromSeconds(opts.TimeoutSeconds)
                });
        });
    }

    // ── Redis resilience pipeline ──────────────────────────────────────────────
    private static void AddRedisPipeline(
        IServiceCollection services,
        NonHttpResilienceOptions opts)
    {
        services.AddResiliencePipeline(ResiliencePipelineNames.Redis, builder =>
        {
            builder
                .AddTimeout(TimeSpan.FromSeconds(opts.TimeoutSeconds))

                .AddCircuitBreaker(new CircuitBreakerStrategyOptions
                {
                    FailureRatio      = opts.FailureRatio,
                    SamplingDuration  = TimeSpan.FromSeconds(opts.SamplingSeconds),
                    MinimumThroughput = 5,
                    BreakDuration     = TimeSpan.FromSeconds(15),
                    ShouldHandle      = new PredicateBuilder()
                        .Handle<StackExchange.Redis.RedisConnectionException>()
                        .Handle<StackExchange.Redis.RedisTimeoutException>()
                        .Handle<TimeoutRejectedException>()
                })

                // Redis retries should be fast — brief fixed delay, no backoff
                .AddRetry(new RetryStrategyOptions
                {
                    MaxRetryAttempts = opts.MaxRetryAttempts,
                    Delay            = TimeSpan.FromMilliseconds(50),
                    BackoffType      = DelayBackoffType.Constant,
                    ShouldHandle     = new PredicateBuilder()
                        .Handle<StackExchange.Redis.RedisConnectionException>()
                        .Handle<StackExchange.Redis.RedisTimeoutException>()
                });
        });
    }

    // ── Message bus resilience pipeline ───────────────────────────────────────
    private static void AddMessageBusPipeline(IServiceCollection services)
    {
        services.AddResiliencePipeline(ResiliencePipelineNames.MessageBus, builder =>
        {
            builder
                .AddTimeout(TimeSpan.FromSeconds(10))

                .AddRetry(new RetryStrategyOptions
                {
                    MaxRetryAttempts = 5,
                    Delay            = TimeSpan.FromSeconds(1),
                    BackoffType      = DelayBackoffType.Exponential,
                    UseJitter        = true,
                    MaxDelay         = TimeSpan.FromSeconds(30),  // cap backoff at 30s
                    ShouldHandle     = new PredicateBuilder()
                        .Handle<Exception>(ex => ex is not OperationCanceledException)
                });
        });
    }

    // ── SQL transient error codes (safe to retry) ─────────────────────────────
    private static bool IsTransientSqlError(int errorNumber) => errorNumber is
        -2      // Timeout
        or 20   // General network error
        or 64   // Connection closed
        or 233  // No process at other end
        or 10053 or 10054 or 10060   // Network errors
        or 40143 or 40197 or 40501 or 40613;  // Azure SQL transient
}