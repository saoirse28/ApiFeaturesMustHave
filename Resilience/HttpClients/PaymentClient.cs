using Microsoft.Extensions.Http.Resilience;
using Polly;
using Resilience.DTOs;
using System.Net.Http.Json;

namespace Resilience.HttpClients;

/// <summary>
/// Typed HTTP client for the external Payment gateway.
///
/// Uses AddStandardResilienceHandler — Microsoft's opinionated default that
/// composes retry + circuit breaker + attempt timeout + total timeout in
/// the correct order. Only override specific options rather than
/// building from scratch.
///
/// IMPORTANT: Never retry non-idempotent POST requests blindly.
/// Payment charges use idempotency keys to ensure safe retries.
/// </summary>
public sealed class PaymentClient
{
    private readonly HttpClient _http;
    private readonly ILogger<PaymentClient> _logger;

    public PaymentClient(HttpClient http, ILogger<PaymentClient> logger)
    {
        _http   = http;
        _logger = logger;
    }

    public async Task<ChargeResult> ChargeAsync(
        ChargeRequest request,
        CancellationToken ct = default)
    {
        // Idempotency key — safe to retry this POST
        _http.DefaultRequestHeaders.TryAddWithoutValidation(
            "Idempotency-Key", request.IdempotencyKey);

        _logger.LogInformation(
            "Charging {Amount:C} for order {OrderId} via payment gateway",
            request.Amount, request.OrderId);

        var response = await _http.PostAsJsonAsync("/v1/charges", request, ct);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<ChargeResult>(ct)
            ?? throw new InvalidOperationException("Empty charge response");
    }

    public async Task<RefundResult> RefundAsync(
        string transactionId,
        decimal amount,
        CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync(
            $"/v1/charges/{transactionId}/refund",
            new { amount },
            ct);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<RefundResult>(ct)
            ?? throw new InvalidOperationException("Empty refund response");
    }

    public async Task<PaymentStatus> GetStatusAsync(
        string transactionId,
        CancellationToken ct = default)
    {
        var response = await _http.GetAsync($"/v1/charges/{transactionId}", ct);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<PaymentStatus>(ct)
            ?? throw new InvalidOperationException("Empty status response");
    }
}