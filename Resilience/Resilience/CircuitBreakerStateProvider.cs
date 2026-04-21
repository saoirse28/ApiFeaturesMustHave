using Polly.CircuitBreaker;
using Polly.Registry;
using Polly.Testing;
using System.Collections.Concurrent;

namespace Resilience.Resilience;

/// <summary>
/// Exposes the current circuit breaker state for each named pipeline.
/// Used by the /circuit-status admin endpoint.
/// </summary>
public interface ICircuitBreakerStateProvider
{
    /// <summary>Returns "Closed", "Open", or "HalfOpen".</summary>
    string GetState(string pipelineName);
}

public sealed class CircuitBreakerStateProvider : ICircuitBreakerStateProvider
{
    private readonly ResiliencePipelineProvider<string> _provider;

    public CircuitBreakerStateProvider(
        ResiliencePipelineProvider<string> provider)
        => _provider = provider;

    public string GetState(string pipelineName)
    {
        try
        {
            var pipeline = _provider.GetPipeline(pipelineName);
            var descriptor = pipeline.GetPipelineDescriptor();

            var cbStrategy = descriptor.Strategies
                .FirstOrDefault(s =>
                    s.Options is CircuitBreakerStrategyOptions);

            if (cbStrategy?.Options is CircuitBreakerStrategyOptions cbOpts)
            {
                // Polly v8 doesn't expose circuit state directly on the pipeline —
                // track it via the OnOpened/OnClosed/OnHalfOpened callbacks
                // registered during pipeline construction (store state externally).
                return CircuitStateTracker.GetState(pipelineName);
            }

            return "Unknown";
        }
        catch
        {
            return "Unknown";
        }
    }
}

/// <summary>
/// Thread-safe in-memory state tracker updated by circuit breaker callbacks.
/// Populated by OnOpened/OnClosed/OnHalfOpened delegates in ResilienceExtensions.
/// </summary>
public static class CircuitStateTracker
{
    private static readonly ConcurrentDictionary<string, string> _states = new();

    public static void SetOpen(string pipeline) => _states[pipeline] = "Open";
    public static void SetClosed(string pipeline) => _states[pipeline] = "Closed";
    public static void SetHalfOpen(string pipeline) => _states[pipeline] = "HalfOpen";

    public static string GetState(string pipeline) =>
        _states.TryGetValue(pipeline, out var state) ? state : "Closed";
}