using Polly.Retry;
using Polly.Telemetry;

namespace Resilience.Resilience;

/// <summary>
/// Bridges Polly v8's built-in telemetry events to your application logger.
/// Polly v8 also natively emits metrics via System.Diagnostics.Metrics,
/// which OpenTelemetry picks up automatically — this listener adds
/// structured log lines to complement the metrics.
///
/// Register via: builder.Services.AddSingleton<ResilienceTelemetryListener>()
/// Then call: services.ConfigureAll<TelemetryOptions>(opts => opts.TelemetryListeners
///     .Add(sp.GetRequiredService<ResilienceTelemetryListener>()))
/// </summary>
public sealed class ResilienceTelemetryListener : TelemetryListener
{
    private readonly ILogger<ResilienceTelemetryListener> _logger;

    public ResilienceTelemetryListener(ILogger<ResilienceTelemetryListener> logger)
        => _logger = logger;

    public override void Write<TResult, TArgs>(in TelemetryEventArguments<TResult, TArgs> args)
    {
        var pipelineName = args.Source.PipelineName
                           ?? "unknown";
        var eventName = args.Event.EventName;

        switch (eventName)
        {
            case "ExecutionAttempt":
                if (args.Event.Severity == ResilienceEventSeverity.Warning)
                {
                    _logger.LogWarning(
                        "[Resilience:{Pipeline}] Attempt {Attempt} failed — " +
                        "duration: {Duration:0}ms, exception: {Exception}",
                        pipelineName,
                        GetAttemptNumber(args),
                        GetDurationMs(args),
                        args.Outcome?.Exception?.Message ?? "no exception");
                }
                break;

            case "OnRetry":
                _logger.LogWarning(
                    "[Resilience:{Pipeline}] RETRY {Attempt} — " +
                    "delay: {DelayMs:0}ms, reason: {Reason}",
                    pipelineName,
                    GetAttemptNumber(args),
                    GetRetryDelayMs(args),
                    args.Outcome?.Exception?.Message ?? "result condition");
                break;

            case "OnCircuitOpened":
                _logger.LogError(
                    "[Resilience:{Pipeline}] CIRCUIT OPENED — " +
                    "downstream service is failing. Break duration logged separately.",
                    pipelineName);
                break;

            case "OnCircuitClosed":
                _logger.LogInformation(
                    "[Resilience:{Pipeline}] CIRCUIT CLOSED — downstream service recovered",
                    pipelineName);
                break;

            case "OnCircuitHalfOpened":
                _logger.LogInformation(
                    "[Resilience:{Pipeline}] CIRCUIT HALF-OPEN — testing probe request",
                    pipelineName);
                break;

            case "OnTimeout":
                _logger.LogWarning(
                    "[Resilience:{Pipeline}] TIMEOUT exceeded — " +
                    "operation exceeded configured limit",
                    pipelineName);
                break;

            case "OnHedging":
                _logger.LogInformation(
                    "[Resilience:{Pipeline}] HEDGED — secondary attempt fired",
                    pipelineName);
                break;
        }
    }

    private static int GetAttemptNumber<TResult, TArgs>(
        in TelemetryEventArguments<TResult, TArgs> args)
    {
        if (args.Arguments is ExecutionAttemptArguments attempt)
            return attempt.AttemptNumber;
        return -1;
    }

    private static double GetDurationMs<TResult, TArgs>(
        in TelemetryEventArguments<TResult, TArgs> args)
    {
        if (args.Arguments is ExecutionAttemptArguments attempt)
            return attempt.Duration.TotalMilliseconds;
        return 0;
    }

    private static double GetRetryDelayMs<TResult, TArgs>(
        in TelemetryEventArguments<TResult, TArgs> args)
    {
        if (args.Arguments is OnRetryArguments<TResult> retry)
            return retry.RetryDelay.TotalMilliseconds;
        return 0;
    }
}