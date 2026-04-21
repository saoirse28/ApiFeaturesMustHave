using APIVersioning.Models;

namespace APIVersioning.Services
{
    public interface IProductService
    {
        public Task<List<Product>> GetAllAsync(CancellationToken ct);
        public Task<Product> GetByIdAsync(string id, CancellationToken ct);
        public Task<(List<Product> list , string next, string prev)> GetCursorPagedAsync(string? cursor, int limit, CancellationToken ct);
    }
}
