using Microsoft.EntityFrameworkCore;
using Zpravodajstvi.Application.Interfaces;
using Zpravodajstvi.Domain.Entities;
using Zpravodajstvi.Infrastructure.Data;

namespace Zpravodajstvi.Infrastructure.Repositories;

public class ArticleRepository : IArticleRepository
{
    private readonly ApplicationDbContext _context;

    public ArticleRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Article>> GetAllAsync(int? categoryId = null, string? tag = null, string? search = null)
    {
        var query = _context.Articles
            .Include(a => a.Category)
            .Include(a => a.Tags)
            .Include(a => a.Images)
            .Include(a => a.Comments)
            .AsNoTracking()
            .AsQueryable();

        if (categoryId.HasValue)
        {
            query = query.Where(a => a.CategoryId == categoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(tag))
        {
            var normalizedTag = tag.Trim().ToLower();
            query = query.Where(a => a.Tags.Any(t => t.Name.ToLower() == normalizedTag));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.Trim().ToLower();
            query = query.Where(a =>
                a.Title.ToLower().Contains(normalizedSearch) ||
                a.Perex.ToLower().Contains(normalizedSearch) ||
                a.Content.ToLower().Contains(normalizedSearch));
        }

        return await query
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
    }

    public async Task<Article?> GetByIdWithDetailsAsync(int id)
    {
        return await _context.Articles
            .Include(a => a.Category)
            .Include(a => a.Tags)
            .Include(a => a.Images)
            .Include(a => a.Comments)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<IEnumerable<Category>> GetCategoriesAsync()
    {
        return await _context.Categories
            .Include(c => c.Articles)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.Articles.AnyAsync(a => a.Id == id);
    }

    public async Task<Article> AddAsync(Article article)
    {
        await _context.Articles.AddAsync(article);
        await _context.SaveChangesAsync();
        return article;
    }

    public async Task<Category> AddCategoryAsync(Category category)
    {
        await _context.Categories.AddAsync(category);
        await _context.SaveChangesAsync();
        return category;
    }

    public async Task UpdateAsync(Article article)
    {
        _context.Articles.Update(article);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var article = await _context.Articles.FindAsync(id);
        if (article != null)
        {
            _context.Articles.Remove(article);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<Category?> GetCategoryByIdAsync(int id)
    {
        return await _context.Categories
            .Include(c => c.Articles)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task DeleteCategoryAsync(int id)
    {
        var category = await _context.Categories.FindAsync(id);
        if (category != null)
        {
            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();
        }
    }
}
