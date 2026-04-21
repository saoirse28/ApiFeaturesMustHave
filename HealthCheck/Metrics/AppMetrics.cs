using System.Diagnostics.Metrics;

namespace HealthCheckMetric.Metrics;

/// <summary>
/// Centralized custom application metrics using System.Diagnostics.Metrics.
/// Register via: builder.Services.AddSingleton<AppMetrics>()
/// Then add the meter name in OpenTelemetry setup: .AddMeter("MyApiHealthCheckMetric.Metrics")
/// </summary>
public sealed class AppMetrics : IDisposable
{
    private readonly Meter _meter;

    // Counters — monotonically increasing values
    public readonly Counter<long> OrdersCreated;
    public readonly Counter<long> PaymentFailures;
    public readonly Counter<long> CacheHits;
    public readonly Counter<long> CacheMisses;

    // Histograms — measure distributions (latency, sizes)
    public readonly Histogram<double> OrderProcessingDuration;
    public readonly Histogram<long> OrderValueAmount;

    // ObservableGauge — sampled at collection time
    public readonly ObservableGauge<int> ActiveConnections;

    private int _activeConnectionCount;

    public AppMetrics()
    {
        _meter = new Meter("MyApiHealthCheckMetric.Metrics", "1.0.0");

        OrdersCreated = _meter.CreateCounter<long>(
            name: "orders.created",
            unit: "{order}",
            description: "Total number of orders created");

        PaymentFailures = _meter.CreateCounter<long>(
            name: "payment.failures",
            unit: "{failure}",
            description: "Total number of payment failures");

        CacheHits = _meter.CreateCounter<long>(
            name: "cache.hits",
            unit: "{hit}",
            description: "Number of cache hits");

        CacheMisses = _meter.CreateCounter<long>(
            name: "cache.misses",
            unit: "{miss}",
            description: "Number of cache misses");

        OrderProcessingDuration = _meter.CreateHistogram<double>(
            name: "orders.processing.duration",
            unit: "ms",
            description: "Time taken to fully process an order");

        OrderValueAmount = _meter.CreateHistogram<long>(
            name: "orders.value",
            unit: "cents",
            description: "Distribution of order values in cents");

        ActiveConnections = _meter.CreateObservableGauge(
            name: "connections.active",
            observeValue: () => _activeConnectionCount,
            unit: "{connection}",
            description: "Currently active client connections");
    }

    public void IncrementConnections() => Interlocked.Increment(ref _activeConnectionCount);
    public void DecrementConnections() => Interlocked.Decrement(ref _activeConnectionCount);

    public void Dispose() => _meter.Dispose();
}