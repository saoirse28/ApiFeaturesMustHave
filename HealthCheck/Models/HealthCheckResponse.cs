namespace HealthCheckMetric.Models
{
    /// <summary>
    /// Typed wrapper for custom /health responses if you need
    /// to return a simpler payload than UIResponseWriter produces.
    /// </summary>
    public record HealthCheckResponse(
        string Status,
        string Version,
        TimeSpan Duration,
        IEnumerable<HealthCheckEntry> Entries);

    public record HealthCheckEntry(
        string Name,
        string Status,
        string? Description,
        long DurationMs,
        IReadOnlyDictionary<string, object> Data);
}
