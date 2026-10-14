using Zpravodajstvi.Domain.Entities;

namespace Zpravodajstvi.Domain.Interfaces;

public interface IArticleRepository : IRepository<Article>
{
    // Výpis článků s volitelným filtrem podle kategorie, štítku, hledaného textu a autora
    Task<IEnumerable<Article>> GetFilteredAsync(int? categoryId = null, string? tag = null, string? search = null, string? authorId = null);
    Task<Article?> GetByIdWithDetailsAsync(int id);
    Task<bool> ExistsAsync(int id);
}
