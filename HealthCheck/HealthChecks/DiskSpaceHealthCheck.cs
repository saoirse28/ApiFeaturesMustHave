using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace HealthCheckMetric.HealthChecks;

public class DiskSpaceHealthCheck : IHealthCheck
{
    // Configurable thresholds (in bytes)
    private const long DegradedThresholdBytes = 1L  * 1024 * 1024 * 1024; // 1 GB
    private const long UnhealthyThresholdBytes = 512L * 1024 * 1024;        // 512 MB

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var drive = DriveInfo.GetDrives()
            .FirstOrDefault(d => d.IsReady && d.Name == Path.GetPathRoot(AppContext.BaseDirectory));

        if (drive is null)
            return Task.FromResult(HealthCheckResult.Unhealthy("Drive not found"));

        var freeBytes = drive.AvailableFreeSpace;
        var totalBytes = drive.TotalSize;
        var usedPercent = Math.Round((1.0 - (double)freeBytes / totalBytes) * 100, 1);

        var data = new Dictionary<string, object>
        {
            ["freeGB"]      = Math.Round(freeBytes / (1024.0 * 1024 * 1024), 2),
            ["totalGB"]     = Math.Round(totalBytes / (1024.0 * 1024 * 1024), 2),
            ["usedPercent"] = usedPercent,
            ["drive"]       = drive.Name
        };

        if (freeBytes < UnhealthyThresholdBytes)
            return Task.FromResult(HealthCheckResult.Unhealthy(
                $"Critically low disk space: {data["freeGB"]} GB free ({usedPercent}% used)",
                data: data));

        if (freeBytes < DegradedThresholdBytes)
            return Task.FromResult(HealthCheckResult.Degraded(
                $"Low disk space warning: {data["freeGB"]} GB free ({usedPercent}% used)",
                data: data));

        return Task.FromResult(HealthCheckResult.Healthy(
            $"Disk OK: {data["freeGB"]} GB free ({usedPercent}% used)",
            data: data));
    }
}