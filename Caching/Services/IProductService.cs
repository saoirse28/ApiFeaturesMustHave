using Caching.Models;

namespace Caching.Services;

public interface IProductService
{
    Task<Product?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<IReadOnlyList<Product>> GetListAsync(string? category, int page, int pageSize, CancellationToken ct = default);
    Task<Product> CreateAsync(CreateProductRequest request, CancellationToken ct = default);
    Task<Product> UpdateAsync(string id, UpdateProductRequest request, CancellationToken ct = default);
    Task DeleteAsync(string id, CancellationToken ct = default);
}