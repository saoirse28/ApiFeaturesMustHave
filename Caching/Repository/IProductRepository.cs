using Caching.Models;

namespace Caching.Repository
{
    public interface IProductRepository
    {
        Task<Product?> FindByIdAsync(string id, CancellationToken ct = default);
        Task<IReadOnlyList<Product>> GetPagedAsync(string? category, int page, int pageSize, CancellationToken ct = default);
        Task<Product> SaveAsync(Product product, CancellationToken ct = default);
        Task<Product> UpdateAsync(Product product, CancellationToken ct = default);
        Task DeleteAsync(string id, CancellationToken ct = default);
    }
}
