using RateLimiting.Model;

namespace RateLimiting.Services
{
    public class ProductCatalogService : IProductCatalogService
    {
        public Task<Product> GetPublicProductAsync(string id, CancellationToken ct)
        {
            return Task.FromResult(
                new Product { Id = int.Parse(id) }
                );
        }

        public Task<IEnumerable<Product>> GetPublicProductsAsync(string? category, CancellationToken ct)
        {
            return Task.FromResult(
                new List<Product>
                {
                    new Product { Id = 1 },
                    new Product { Id = 2 },
                    new Product { Id = 3 }
                }.AsEnumerable()
                );
        }

        public Task<IEnumerable<Product>> SearchAsync(string query, CancellationToken ct)
        {
            return Task.FromResult(
                new List<Product>
                {
                    new Product { Id = 1 },
                    new Product { Id = 2 },
                    new Product { Id = 3 }
                }.AsEnumerable()
                );
        }
    }
}
