using RateLimiting.Model;

namespace RateLimiting.Services
{
    public interface IProductCatalogService
    {
        public Task<IEnumerable<Product>> GetPublicProductsAsync(string? category, CancellationToken ct);
        public Task<Product> GetPublicProductAsync(string id, CancellationToken ct);
        public Task<IEnumerable<Product>> SearchAsync(string query, CancellationToken ct);
    }
}
