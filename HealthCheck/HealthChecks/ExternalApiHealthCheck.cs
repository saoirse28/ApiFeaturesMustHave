using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace HealthCheckMetric.HealthChecks;

public class ExternalApiHealthCheck : IHealthCheck
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;

    // Typed HttpClient injected via IHttpClientFactory
    public ExternalApiHealthCheck(IHttpClientFactory factory, IConfiguration config)
    {
        _httpClient = factory.CreateClient("HealthCheckClient");
        _config = config;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var url = _config["ExternalApi:HealthUrl"] ?? "https://api.example.com/ping";
        var sw = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(5)); // Hard timeout

            var response = await _httpClient.GetAsync(url, cts.Token);
            sw.Stop();

            var data = new Dictionary<string, object>
            {
                ["url"] = url,
                ["statusCode"] = (int)response.StatusCode,
                ["responseTimeMs"] = sw.ElapsedMilliseconds
            };

            if (!response.IsSuccessStatusCode)
            {
                return HealthCheckResult.Degraded(
                    $"External API returned {(int)response.StatusCode}",
                    data: data);
            }

            return sw.ElapsedMilliseconds > 2000
                ? HealthCheckResult.Degraded("External API slow", data: data)
                : HealthCheckResult.Healthy("External API OK", data: data);
        }
        catch (OperationCanceledException)
        {
            return HealthCheckResult.Degraded("External API timed out");
        }
        catch (HttpRequestException ex)
        {
            return HealthCheckResult.Unhealthy("External API unreachable", ex);
        }
    }
}