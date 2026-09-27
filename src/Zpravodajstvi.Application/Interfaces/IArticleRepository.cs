using Zpravodajstvi.Domain.Entities;

namespace Zpravodajstvi.Application.Interfaces;

public interface IArticleRepository
{
    Task<IEnumerable<Article>> GetAllAsync(int? categoryId = null, string? tag = null, string? search = null);
    Task<Article?> GetByIdWithDetailsAsync(int id);
    Task<IEnumerable<Category>> GetCategoriesAsync();
    Task<bool> ExistsAsync(int id);
}
