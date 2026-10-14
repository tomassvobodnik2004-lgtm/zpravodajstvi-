using Microsoft.EntityFrameworkCore;
using Zpravodajstvi.Domain.Interfaces;
using Zpravodajstvi.Domain.Entities;
using Zpravodajstvi.Domain.Search;
using Zpravodajstvi.Infrastructure.Data;

namespace Zpravodajstvi.Infrastructure.Repositories;

public class ArticleRepository : Repository<Article>, IArticleRepository
{
    public ArticleRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Article>> GetFilteredAsync(int? categoryId = null, string? tag = null, string? search = null, string? authorId = null)
    {
        var query = _context.Articles
            .Include(a => a.Category)
            .Include(a => a.Author)
            .Include(a => a.Tags)
            .Include(a => a.Images)
            .Include(a => a.Comments)
            .AsNoTracking()
            .AsQueryable();

        if (categoryId.HasValue)
        {
            query = query.Where(a => a.CategoryId == categoryId.Value);
        }

        if (!string.IsNullOrEmpty(authorId))
        {
            query = query.Where(a => a.AuthorId == authorId);
        }

        // Porovnává se s normalizovanými sloupci (Search*), ne přes SQL lower(),
        // který v SQLite převádí jen písmena ASCII ("Č" by zůstalo velké)
        if (!string.IsNullOrWhiteSpace(tag))
        {
            var normalizedTag = NormalizeTag(tag);
            query = query.Where(a => a.Tags.Any(t => t.SearchName == normalizedTag));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var trimmedSearch = search.Trim();
            if (trimmedSearch.StartsWith('#'))
            {
                // Hledání hashtagu (#Česko) funguje stejně jako filtr podle štítku
                var normalizedTag = NormalizeTag(trimmedSearch);
                query = query.Where(a => a.Tags.Any(t => t.SearchName == normalizedTag));
            }
            else
            {
                var normalizedSearch = SearchNormalizer.Normalize(trimmedSearch);
                query = query.Where(a =>
                    a.SearchTitle.Contains(normalizedSearch) ||
                    a.SearchPerex.Contains(normalizedSearch) ||
                    a.SearchContent.Contains(normalizedSearch));
            }
        }

        return await query
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
    }

    // Detail článku včetně autora článku i autorů komentářů (ThenInclude), aby se zobrazilo jméno, ne jen Id
    public async Task<Article?> GetByIdWithDetailsAsync(int id)
    {
        return await _context.Articles
            .Include(a => a.Category)
            .Include(a => a.Author)
            .Include(a => a.Tags)
            .Include(a => a.Images)
            .Include(a => a.Comments)
                .ThenInclude(c => c.User)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.Articles.AnyAsync(a => a.Id == id);
    }

    // "#Česko", "česko" i "ČESKO" -> "cesko"
    private static string NormalizeTag(string tag)
    {
        return SearchNormalizer.Normalize(tag.Trim().TrimStart('#').Trim());
    }
}
