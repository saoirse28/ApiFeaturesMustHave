using APIVersioning.Models;

namespace APIVersioning.Services
{
    public class CategoryService : ICategoryService
    {
        public Task<IEnumerable<Category>> GetAllAsync(CancellationToken ct)
        {
            throw new NotImplementedException();
        }

        public Task<Category?> GetByIdAsync(string id, CancellationToken ct)
        {
            throw new NotImplementedException();
        }

        public Task<CategoryTree> GetTreeAsync(CancellationToken ct)
        {
            throw new NotImplementedException();
        }
    }
}
