using Zpravodajstvi.Domain.Entities;

namespace Zpravodajstvi.Domain.Interfaces;

public interface ICategoryRepository : IRepository<Category>
{
    // Kategorie včetně článků (kvůli počtu článků v kategorii)
    Task<IEnumerable<Category>> GetAllWithArticlesAsync();
    Task<Category?> GetByIdWithArticlesAsync(int id);
}
