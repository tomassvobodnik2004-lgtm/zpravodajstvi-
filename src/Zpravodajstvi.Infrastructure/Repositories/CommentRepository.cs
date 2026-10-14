using Microsoft.EntityFrameworkCore;
using Zpravodajstvi.Domain.Interfaces;
using Zpravodajstvi.Domain.Entities;
using Zpravodajstvi.Infrastructure.Data;

namespace Zpravodajstvi.Infrastructure.Repositories;

public class CommentRepository : Repository<Comment>, ICommentRepository
{
    public CommentRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Comment>> GetByArticleIdAsync(int articleId)
    {
        return await _context.Comments
            .Include(c => c.User)
            .Where(c => c.ArticleId == articleId)
            .OrderByDescending(c => c.CreatedAt)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IEnumerable<Comment>> GetByUserIdAsync(string userId)
    {
        return await _context.Comments
            .Include(c => c.Article)
            .Include(c => c.User)
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.CreatedAt)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IEnumerable<Comment>> GetAllWithDetailsAsync()
    {
        return await _context.Comments
            .Include(c => c.Article)
            .Include(c => c.User)
            .OrderByDescending(c => c.CreatedAt)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<Comment?> GetByIdWithDetailsAsync(int id)
    {
        return await _context.Comments
            .Include(c => c.Article)
            .Include(c => c.User)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id);
    }
}
