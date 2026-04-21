using APIVersioning.DTOs.V3;
using APIVersioning.Services;
using Asp.Versioning;

namespace APIVersioning.MinimalApis;

/// <summary>
/// Demonstrates versioning with Minimal APIs (alternative to controllers).
/// Uses IVersionedEndpointRouteBuilder for grouping endpoints per version.
/// </summary>
public static class ProductEndpoints
{
    public static WebApplication MapProductEndpoints(this WebApplication app)
    {
        var apiVersionSet = app.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1, 0))
            .HasApiVersion(new ApiVersion(2, 0))
            .HasApiVersion(new ApiVersion(3, 0))
            .ReportApiVersions()
            .Build();

        // ── V1 ────────────────────────────────────────────────────────────────
        var v1 = app.MapGroup("/api/v{version:apiVersion}/products")
            .WithApiVersionSet(apiVersionSet)
            .MapToApiVersion(1, 0)
            .WithTags("Products");

        v1.MapGet("/", async (IProductService products, CancellationToken ct) =>
        {
            var list = await products.GetAllAsync(ct);
            return Results.Ok(list.Select(p => new { p.Id, p.Name, p.Price }));
        })
        .WithName("GetProductsV1")
        .WithSummary("Get all products (V1 — deprecated)");

        v1.MapGet("/{id}", async (string id, IProductService products, CancellationToken ct) =>
        {
            var product = await products.GetByIdAsync(id, ct);
            return product is null
                ? Results.NotFound()
                : Results.Ok(new { product.Id, product.Name, product.Price });
        })
        .WithName("GetProductByIdV1");

        // ── V2 ────────────────────────────────────────────────────────────────
        var v2 = app.MapGroup("/api/v{version:apiVersion}/products")
            .WithApiVersionSet(apiVersionSet)
            .MapToApiVersion(2, 0)
            .WithTags("Products");

        v2.MapGet("/", async (IProductService products, CancellationToken ct) =>
        {
            var list = await products.GetAllAsync(ct);
            return Results.Ok(list.Select(p => new
            {
                p.Id,
                p.Name,
                Price = new { p.Price, Currency = "USD" },
                p.CategoryId,
                p.StockLevel
            }));
        })
        .WithName("GetProductsV2")
        .WithSummary("Get all products with inventory (V2)");

        // ── V3 ────────────────────────────────────────────────────────────────
        var v3 = app.MapGroup("/api/v{version:apiVersion}/products")
            .WithApiVersionSet(apiVersionSet)
            .MapToApiVersion(3, 0)
            .WithTags("Products");

        v3.MapGet("/", async (
            string? cursor,
            int limit,
            IProductService products,
            CancellationToken ct) =>
        {
            limit = Math.Clamp(limit, 1, 100);
            var (list, next, prev) = await products.GetCursorPagedAsync(cursor, limit, ct);
            return Results.Ok(new CursorPagedResponse<object>(
                list.Select(p => (object)new
                {
                    p.Id,
                    p.Name,
                    p.Slug,
                    Price = new { p.Price, Currency = "USD" },
                    p.CategoryId,
                    p.StockLevel,
                    p.Rating,
                    p.ReviewCount
                }).ToList(),
                new CursorMeta(next, prev, limit, next is not null)));
        })
        .WithName("GetProductsV3")
        .WithSummary("Get products with cursor pagination (V3 — current)");

        return app;
    }
}