using Microsoft.EntityFrameworkCore;
using Zpravodajstvi.Domain.Entities;
using Zpravodajstvi.Domain.Interfaces;
using Zpravodajstvi.Infrastructure.Data;

namespace Zpravodajstvi.Infrastructure.Repositories;

public class CategoryRepository : Repository<Category>, ICategoryRepository
{
    public CategoryRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Category>> GetAllWithArticlesAsync()
    {
        return await _context.Categories
            .Include(c => c.Articles)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<Category?> GetByIdWithArticlesAsync(int id)
    {
        return await _context.Categories
            .Include(c => c.Articles)
            .FirstOrDefaultAsync(c => c.Id == id);
    }
}
