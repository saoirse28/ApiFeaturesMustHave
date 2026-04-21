using Caching.Caching;
using Caching.Models;
using Caching.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace Caching.Controllers;

[ApiController]
[Route("api/v1/products")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _products;
    private readonly IOutputCacheStore _outputCacheStore;

    public ProductsController(
        IProductService products,
        IOutputCacheStore outputCacheStore)
    {
        _products        = products;
        _outputCacheStore = outputCacheStore;
    }

    // ── GET /api/v1/products ──────────────────────────────────────────────────
    // Output cache: 5 min, varies by query params, tagged "Products"
    [HttpGet]
    [OutputCache(PolicyName = CachePolicies.Products)]
    public async Task<IActionResult> GetList(
        [FromQuery] string? category,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var products = await _products.GetListAsync(category, page, pageSize, ct);
        return Ok(products);
    }

    // ── GET /api/v1/products/{id} ─────────────────────────────────────────────
    // CachedProductService handles Redis caching transparently
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var product = await _products.GetByIdAsync(id, ct);
        return product is null ? NotFound() : Ok(product);
    }

    // ── POST /api/v1/products ─────────────────────────────────────────────────
    [HttpPost]
    [OutputCache(PolicyName = CachePolicies.NoCache)]
    public async Task<IActionResult> Create(
        [FromBody] CreateProductRequest request,
        CancellationToken ct)
    {
        var product = await _products.CreateAsync(request, ct);

        // Evict output-cached product list responses after a write
        await _outputCacheStore.EvictByTagAsync(CachePolicies.Products, ct);

        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
    }

    // ── PUT /api/v1/products/{id} ─────────────────────────────────────────────
    [HttpPut("{id}")]
    [OutputCache(PolicyName = CachePolicies.NoCache)]
    public async Task<IActionResult> Update(
        string id,
        [FromBody] UpdateProductRequest request,
        CancellationToken ct)
    {
        var product = await _products.UpdateAsync(id, request, ct);

        // Evict output cache for product lists
        await _outputCacheStore.EvictByTagAsync(CachePolicies.Products, ct);

        return Ok(product);
    }

    // ── DELETE /api/v1/products/{id} ──────────────────────────────────────────
    [HttpDelete("{id}")]
    [OutputCache(PolicyName = CachePolicies.NoCache)]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        await _products.DeleteAsync(id, ct);

        // Evict all product output cache entries
        await _outputCacheStore.EvictByTagAsync(CachePolicies.Products, ct);

        return NoContent();
    }
}
