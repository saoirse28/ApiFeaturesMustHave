using APIVersioning.Models;

namespace APIVersioning.Services
{
    public interface ICategoryService
    {
        public Task<IEnumerable<Category>> GetAllAsync(CancellationToken ct);
        public Task<Category?> GetByIdAsync(string id, CancellationToken ct);
        public Task<CategoryTree> GetTreeAsync(CancellationToken ct);   
    }
}
