using Zpravodajstvi.Domain.Entities;

namespace Zpravodajstvi.Domain.Interfaces;

public interface IArticleRepository
{
    Task<IEnumerable<Article>> GetAllAsync(int? categoryId = null, string? tag = null, string? search = null);
    Task<Article?> GetByIdWithDetailsAsync(int id);
    Task<IEnumerable<Category>> GetCategoriesAsync();
    Task<bool> ExistsAsync(int id);
    Task<Article> AddAsync(Article article);
    Task<Category> AddCategoryAsync(Category category);
    Task UpdateAsync(Article article);
    Task DeleteAsync(int id);
    Task<Category?> GetCategoryByIdAsync(int id);
    Task DeleteCategoryAsync(int id);
}
