using Zpravodajstvi.Application.DTOs;

namespace Zpravodajstvi.Application.Interfaces;

public interface IArticleService
{
    Task<IEnumerable<ArticleListDto>> GetArticlesAsync(int? categoryId = null, string? tag = null, string? search = null);
    Task<IEnumerable<ArticleListDto>> GetArticlesByAuthorAsync(string authorId);
    Task<ArticleDetailDto?> GetArticleDetailsAsync(int id);
    // authorId = Id přihlášeného uživatele, controller ho bere z claimu NameIdentifier (nikdy z formuláře)
    Task<ArticleDetailDto> CreateArticleAsync(CreateArticleDto dto, string authorId);
    Task UpdateArticleAsync(UpdateArticleDto dto);
    Task DeleteArticleAsync(int id);
}
