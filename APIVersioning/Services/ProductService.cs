using APIVersioning.Models;

namespace APIVersioning.Services
{
    public class ProductService : IProductService
    {
        public Task<List<Product>> GetAllAsync(CancellationToken ct)
        {
            throw new NotImplementedException();
        }

        public Task<Product> GetByIdAsync(string id, CancellationToken ct)
        {
            throw new NotImplementedException();
        }

        public Task<(List<Product> list, string next, string prev)> GetCursorPagedAsync(string? cursor, int limit, CancellationToken ct)
        {
            throw new NotImplementedException();
        }
    }
}
