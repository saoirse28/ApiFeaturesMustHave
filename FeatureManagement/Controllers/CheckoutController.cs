using FeatureManagement.DTOs;
using FeatureManagement.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.Mvc;

namespace FeatureManagement.Controllers;

[ApiController]
[Route("api/v1/checkout")]
public class CheckoutController : ControllerBase
{
    private readonly ICheckoutService _checkout;
    private readonly IFeatureManager _features;
    private readonly ILogger<CheckoutController> _logger;

    public CheckoutController(
        ICheckoutService checkout,
        IFeatureManager features,
        ILogger<CheckoutController> logger)
    {
        _checkout = checkout;
        _features = features;
        _logger   = logger;
    }

    // Standard checkout — routes internally via CheckoutServiceFactory
    [HttpPost]
    public async Task<IActionResult> Checkout(
        [FromBody] CheckoutRequest request,
        CancellationToken ct)
    {
        var result = await _checkout.CheckoutAsync(request.Cart, ct);
        return Ok(result);
    }

    // ── [FeatureGate] attribute — returns 404 if flag is disabled ─────────────

    /// <summary>
    /// One-click checkout endpoint — only routable when flag is enabled.
    /// Returns HTTP 404 when disabled (as if the endpoint doesn't exist).
    /// Use IDisabledFeaturesHandler to change the disabled behavior.
    /// </summary>
    [HttpPost("one-click")]
    [FeatureGate(FeatureFlags.FeatureFlags.OneClickCheckout)]
    
    public async Task<IActionResult> OneClick(
        [FromBody] OneClickRequest request,
        CancellationToken ct)
    {
        var result = await _checkout.QuickCheckoutAsync(request, ct);
        return Ok(result);
    }

    // ── Manual flag check — richer conditional logic ──────────────────────────

    [HttpGet("payment-options")]
    public async Task<IActionResult> GetPaymentOptions(CancellationToken ct)
    {
        var options = await _checkout.GetPaymentOptionsAsync(ct);
        return Ok(options);
    }

    // ── Maintenance mode kill switch ──────────────────────────────────────────

    [HttpGet("status")]
    public async Task<IActionResult> GetStatus()
    {
        if (await _features.IsEnabledAsync(FeatureFlags.FeatureFlags.MaintenanceMode))
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { message = "Checkout is temporarily unavailable. Please try again later." });
        }

        return Ok(new { status = "available" });
    }
}