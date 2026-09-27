using Zpravodajstvi.Application.DTOs;

namespace Zpravodajstvi.Application.Interfaces;

public interface IArticleService
{
    Task<IEnumerable<ArticleListDto>> GetArticlesAsync(int? categoryId = null, string? tag = null, string? search = null);
    Task<ArticleDetailDto?> GetArticleDetailsAsync(int id);
    Task<IEnumerable<CategoryDto>> GetCategoriesAsync();
    Task<ArticleDetailDto> CreateArticleAsync(CreateArticleDto dto);
    Task UpdateArticleAsync(int id, string title, string content, int categoryId, string? imageUrl = null);
    Task DeleteArticleAsync(int id);
}
