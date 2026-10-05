using Microsoft.EntityFrameworkCore;
using Zpravodajstvi.Domain.Interfaces;
using Zpravodajstvi.Domain.Entities;
using Zpravodajstvi.Infrastructure.Data;

namespace Zpravodajstvi.Infrastructure.Repositories;

public class CommentRepository : ICommentRepository
{
    private readonly ApplicationDbContext _context;

    public CommentRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Comment>> GetByArticleIdAsync(int articleId)
    {
        return await _context.Comments
            .Where(c => c.ArticleId == articleId)
            .OrderByDescending(c => c.CreatedAt)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<Comment> AddAsync(Comment comment)
    {
        await _context.Comments.AddAsync(comment);
        await _context.SaveChangesAsync();
        return comment;
    }
    public async Task<Comment> UpdateAsync(Comment comment)
    {
        _context.Comments.Update(comment);
        await _context.SaveChangesAsync();
        return comment;
    }
    public async Task<Comment> DeleteAsync(Comment comment)
    {
        _context.Comments.Remove(comment);
        await _context.SaveChangesAsync();
        return comment;
    }
    public async Task<Comment?> GetByIdAsync(int id)
    {
        return await _context.Comments.FindAsync(id);
    }
}
