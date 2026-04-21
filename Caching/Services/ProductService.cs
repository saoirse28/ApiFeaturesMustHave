using Caching.Models;
using Caching.Repository;
using Caching.Services;

/// <summary>
/// The real implementation — hits the database.
/// The caching decorator wraps this transparently.
/// </summary>
public sealed class ProductService : IProductService
{
    private readonly IProductRepository _repo;
    private readonly ILogger<ProductService> _logger;

    public ProductService(IProductRepository repo, ILogger<ProductService> logger)
    {
        _repo   = repo;
        _logger = logger;
    }

    public async Task<Product?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        _logger.LogDebug("DB: fetching product {Id}", id);
        return await _repo.FindByIdAsync(id, ct);
    }

    public async Task<IReadOnlyList<Product>> GetListAsync(
        string? category, int page, int pageSize, CancellationToken ct = default)
    {
        _logger.LogDebug("DB: fetching product list category={Category} page={Page}", category, page);
        return await _repo.GetPagedAsync(category, page, pageSize, ct);
    }

    public async Task<Product> CreateAsync(CreateProductRequest request, CancellationToken ct = default)
    {
        var product = new Product(request);
        await _repo.SaveAsync(product, ct);
        return product;
    }

    public async Task<Product> UpdateAsync(string id, UpdateProductRequest request, CancellationToken ct = default)
    {
        var product = await _repo.FindByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Product {id} not found");
        product.Apply(request);
        await _repo.UpdateAsync(product, ct);
        return product;
    }

    public async Task DeleteAsync(string id, CancellationToken ct = default)
    {
        await _repo.DeleteAsync(id, ct);
    }
}
