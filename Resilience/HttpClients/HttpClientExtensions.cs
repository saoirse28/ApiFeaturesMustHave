using Polly;
using Polly.CircuitBreaker;
using Polly.Hedging;
using Polly.Retry;
using Resilience.HttpClients;
using Resilience.Resilience;

namespace ResilienceDemo.HttpClients;

/// <summary>
/// Extension methods that register all typed HTTP clients
/// with their respective resilience strategies.
/// </summary>
public static class HttpClientExtensions
{
    public static IServiceCollection AddHttpClients(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var settings = configuration
            .GetSection("Resilience")
            .Get<ResilienceSettings>() ?? new ResilienceSettings();

        // ── Payment client — standard handler, tuned for payment semantics ────
        services
            .AddHttpClient<PaymentClient>(client =>
            {
                client.BaseAddress = new Uri(
                    configuration["Services:PaymentGateway:BaseUrl"]
                    ?? "https://api.payment.example.com");
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                client.Timeout = Timeout.InfiniteTimeSpan; // let Polly control timeouts
            })
            .AddStandardResilienceHandler(opts =>
            {
                var s = settings.Payment;

                // Per-attempt timeout — each individual try
                opts.AttemptTimeout.Timeout =
                    TimeSpan.FromSeconds(s.AttemptTimeoutSeconds);

                // Retry — exponential backoff with jitter
                opts.Retry.MaxRetryAttempts = s.MaxRetryAttempts;
                opts.Retry.Delay            = TimeSpan.FromMilliseconds(s.RetryDelayMs);
                opts.Retry.BackoffType      = DelayBackoffType.Exponential;
                opts.Retry.UseJitter        = true;

                // Only retry idempotent HTTP methods — NEVER retry a raw POST
                // (payment uses idempotency keys so retrying POST is safe here)
                opts.Retry.ShouldHandle = args =>
                    ValueTask.FromResult(
                        args.Outcome.Exception is HttpRequestException ||
                        args.Outcome.Result?.StatusCode is
                            System.Net.HttpStatusCode.RequestTimeout or
                            System.Net.HttpStatusCode.TooManyRequests or
                            System.Net.HttpStatusCode.ServiceUnavailable or
                            System.Net.HttpStatusCode.GatewayTimeout);

                opts.Retry.OnRetry = args =>
                {
                    Console.WriteLine(
                        $"[Payment] Retry {args.AttemptNumber + 1}/{s.MaxRetryAttempts} " +
                        $"after {args.RetryDelay.TotalMilliseconds:0}ms — " +
                        $"status: {args.Outcome.Result?.StatusCode}, " +
                        $"error: {args.Outcome.Exception?.Message}");
                    return ValueTask.CompletedTask;
                };

                // Circuit breaker — trips when 50% of calls fail
                opts.CircuitBreaker.FailureRatio =
                    s.CircuitBreakerFailureRatio;
                opts.CircuitBreaker.SamplingDuration =
                    TimeSpan.FromSeconds(s.CircuitBreakerSamplingSeconds);
                opts.CircuitBreaker.MinimumThroughput =
                    s.CircuitBreakerMinThroughput;
                opts.CircuitBreaker.BreakDuration =
                    TimeSpan.FromSeconds(s.CircuitBreakerBreakSeconds);

                opts.CircuitBreaker.OnOpened = args =>
                {
                    Console.WriteLine(
                        $"[Payment] Circuit OPENED for {args.BreakDuration.TotalSeconds}s — " +
                        $"payment gateway unreachable");
                    return ValueTask.CompletedTask;
                };

                opts.CircuitBreaker.OnClosed = _ =>
                {
                    Console.WriteLine("[Payment] Circuit CLOSED — gateway recovered");
                    return ValueTask.CompletedTask;
                };

                // Total request timeout (all retries included)
                opts.TotalRequestTimeout.Timeout =
                    TimeSpan.FromSeconds(s.TotalTimeoutSeconds);
            });

        // ── Inventory client — custom pipeline with fallback-friendly circuit ──
        services
            .AddHttpClient<InventoryClient>(client =>
            {
                client.BaseAddress = new Uri(
                    configuration["Services:Inventory:BaseUrl"]
                    ?? "https://inventory.internal.example.com");
                client.Timeout = Timeout.InfiniteTimeSpan;
            })
            .AddResilienceHandler(
                ResiliencePipelineNames.Inventory,
                builder =>
                {
                    builder
                        .AddTimeout(TimeSpan.FromSeconds(8))

                        .AddCircuitBreaker(new CircuitBreakerStrategyOptions<HttpResponseMessage>
                        {
                            FailureRatio      = 0.6,
                            SamplingDuration  = TimeSpan.FromSeconds(20),
                            MinimumThroughput = 5,
                            BreakDuration     = TimeSpan.FromSeconds(20),
                            ShouldHandle      = args => ValueTask.FromResult(
                                args.Outcome.Exception is HttpRequestException ||
                                (int)(args.Outcome.Result?.StatusCode ?? 0) >= 500)
                        })

                        .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
                        {
                            MaxRetryAttempts = 2,
                            Delay            = TimeSpan.FromMilliseconds(150),
                            BackoffType      = DelayBackoffType.Linear,
                            ShouldHandle     = args => ValueTask.FromResult(
                                args.Outcome.Exception is HttpRequestException ||
                                args.Outcome.Result?.StatusCode ==
                                    System.Net.HttpStatusCode.ServiceUnavailable)
                        })

                        .AddTimeout(TimeSpan.FromSeconds(3));
                });

        // ── Notification client — hedging strategy ────────────────────────────
        services
            .AddHttpClient<NotificationClient>(client =>
            {
                client.BaseAddress = new Uri(
                    configuration["Services:Notifications:BaseUrl"]
                    ?? "https://notifications.internal.example.com");
                client.Timeout = Timeout.InfiniteTimeSpan;
            })
            .AddResilienceHandler(
                ResiliencePipelineNames.Notification,
                builder =>
                {
                    builder
                        .AddTimeout(TimeSpan.FromSeconds(5))

                        // Hedging: if no response in 800ms, fire a second request in parallel.
                        // Use whichever responds first. Ideal for non-idempotent-safe
                        // notifications where latency matters more than duplicate suppression.
                        .AddHedging(new HedgingStrategyOptions<HttpResponseMessage>
                        {
                            MaxHedgedAttempts = 2,
                            Delay             = TimeSpan.FromMilliseconds(800),
                            ShouldHandle      = args => ValueTask.FromResult(
                                args.Outcome.Exception is not null ||
                                (int)(args.Outcome.Result?.StatusCode ?? 0) >= 500),
                            OnHedging = args =>
                            {
                                Console.WriteLine(
                                    $"[Notification] Hedging — attempt {args.AttemptNumber + 1} ");
                                return ValueTask.CompletedTask;
                            }
                        });
                });

        return services;
    }
}
