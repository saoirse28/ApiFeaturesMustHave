using Polly.CircuitBreaker;
using Resilience.DTOs;
using System.Net.Http.Json;

namespace Resilience.HttpClients;

/// <summary>
/// Typed HTTP client for the internal Inventory service.
/// Uses a custom pipeline with stricter timeouts and
/// a fallback that returns a default "check later" response
/// instead of propagating failures to the caller.
/// </summary>
public sealed class InventoryClient
{
    private readonly HttpClient _http;
    private readonly ILogger<InventoryClient> _logger;

    public InventoryClient(HttpClient http, ILogger<InventoryClient> logger)
    {
        _http   = http;
        _logger = logger;
    }

    /// <summary>
    /// Checks stock level. Returns null if the inventory service
    /// is unavailable — callers treat null as "assume available".
    /// </summary>
    public async Task<StockLevel?> GetStockLevelAsync(
        string productId,
        CancellationToken ct = default)
    {
        try
        {
            var response = await _http.GetAsync($"/api/stock/{productId}", ct);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<StockLevel>(ct);
        }
        catch (BrokenCircuitException ex)
        {
            // Circuit is open — inventory service is known-down
            // Return a soft degraded response rather than failing the whole order
            _logger.LogWarning(
                "Inventory circuit open for product {ProductId} — " +
                "assuming available. Circuit reopens at {ResetTime}",
                productId, ex.Message);
            return new StockLevel(productId, quantity: -1, available: true, isEstimated: true);
        }
    }

    public async Task<bool> ReserveStockAsync(
        string productId,
        int quantity,
        string orderId,
        CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync(
            "/api/stock/reserve",
            new { productId, quantity, orderId },
            ct);

        return response.IsSuccessStatusCode;
    }
}
