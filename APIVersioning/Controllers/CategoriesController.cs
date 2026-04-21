using APIVersioning.Services;
using APIVersioning.Versioning;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace APIVersioning.Controllers;

/// <summary>
/// Alternative pattern: a single controller class that serves
/// multiple API versions using version-mapped action methods.
///
/// Best for: endpoints that are identical across versions, with
/// only one or two actions that differ between versions.
///
/// Drawback: becomes unwieldy for large contracts — prefer
/// separate controllers in separate namespaces for major changes.
/// </summary>
[ApiController]
[ApiVersion(ApiVersions.V2)]
[ApiVersion(ApiVersions.V3)]
[Route("api/v{version:apiVersion}/categories")]
public sealed class CategoriesController : ControllerBase
{
    private readonly ICategoryService _categories;

    public CategoriesController(ICategoryService categories)
        => _categories = categories;

    // ── Identical across V2 and V3 — no MapToApiVersion needed ───────────────

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var categories = await _categories.GetAllAsync(ct);
        return Ok(categories);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var category = await _categories.GetByIdAsync(id, ct);
        return category is null ? NotFound() : Ok(category);
    }

    // ── V3-only action — V2 requests return 404 for this endpoint ─────────────

    /// <summary>Returns a category tree (V3 only).</summary>
    [HttpGet("tree")]
    [MapToApiVersion(ApiVersions.V3)]
    public async Task<IActionResult> GetTree(CancellationToken ct)
    {
        var tree = await _categories.GetTreeAsync(ct);
        return Ok(tree);
    }

    // ── Deprecated V2-only action — removed in V3 ─────────────────────────────

    /// <summary>Legacy flat category list (V2 only — removed in V3).</summary>
    [HttpGet("flat")]
    [MapToApiVersion(ApiVersions.V2)]
    [Obsolete]
    public async Task<IActionResult> GetFlat(CancellationToken ct)
    {
        var categories = await _categories.GetAllAsync(ct);
        return Ok(categories);
    }
}