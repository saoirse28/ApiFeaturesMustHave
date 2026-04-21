using Caching.Models;

namespace Caching.Repository
{
    public class ProductRepository : IProductRepository
    {
        public Task DeleteAsync(string id, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public Task<Product?> FindByIdAsync(string id, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyList<Product>> GetPagedAsync(string? category, int page, int pageSize, CancellationToken ct = default)
        {
            var productList = new List<Product>();
            var totalProducts = 100; // Simulate total products in the database
            for (int i = 0; i < pageSize; i++)
            {
                var productId = ((page - 1) * pageSize + i + 1).ToString();
                if (int.Parse(productId) > totalProducts)
                    break;
                productList.Add(new Product( new CreateProductRequest 
                {
                    
                    Name = $"Product {productId}",
                    Description = $"Description for Product {productId}",
                    Price = decimal.Parse(productId) * 10,
                    Category = category ?? "General"
                }));
            }
            
            return Task.FromResult<IReadOnlyList<Product>>(productList);
        }

        public Task<Product> SaveAsync(Product product, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public Task<Product> UpdateAsync(Product product, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }
    }
}
