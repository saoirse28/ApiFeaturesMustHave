using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using RateLimiting.RateLimiting;
using RateLimiting.Services;

namespace RateLimiting.Controllers;

/// <summary>
/// Public-facing endpoints accessible without authentication.
/// Uses anonymous (IP-based) rate limiting and token bucket for search.
/// </summary>
[ApiController]
[Route("api/v1/public")]
[EnableRateLimiting(RateLimitPolicies.AnonymousPublic)]
public class PublicController : ControllerBase
{
    private readonly IProductCatalogService _catalog;

    public PublicController(IProductCatalogService catalog) => _catalog = catalog;

    [HttpGet("products")]
    public async Task<IActionResult> GetProducts(
        [FromQuery] string? category,
        CancellationToken ct)
        => Ok(await _catalog.GetPublicProductsAsync(category, ct));

    [HttpGet("products/{id}")]
    public async Task<IActionResult> GetProduct(string id, CancellationToken ct)
    {
        var product = await _catalog.GetPublicProductAsync(id, ct);
        return product is null ? NotFound() : Ok(product);
    }

    // Search uses token bucket — allows bursts for typeahead but caps sustained load
    [HttpGet("search")]
    [EnableRateLimiting(RateLimitPolicies.Search)]   // overrides controller-level policy
    public async Task<IActionResult> Search(
        [FromQuery] string q,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
            return BadRequest(new { error = "Query must be at least 2 characters" });

        var results = await _catalog.SearchAsync(q, ct);
        return Ok(results);
    }

    // Health check — explicitly exempt from rate limiting
    [HttpGet("ping")]
    [DisableRateLimiting]
    public IActionResult Ping() => Ok(new { status = "ok", timestamp = DateTimeOffset.UtcNow });
}