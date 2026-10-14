using Microsoft.EntityFrameworkCore;
using Zpravodajstvi.Domain.Entities;
using Zpravodajstvi.Domain.Interfaces;
using Zpravodajstvi.Infrastructure.Data;

namespace Zpravodajstvi.Infrastructure.Repositories;

public class TagRepository : Repository<Tag>, ITagRepository
{
    public TagRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Tag>> GetAllWithArticlesAsync()
    {
        return await _context.Tags
            .Include(t => t.Articles)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<Tag?> GetByIdWithArticlesAsync(int id)
    {
        return await _context.Tags
            .Include(t => t.Articles)
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id);
    }
}
