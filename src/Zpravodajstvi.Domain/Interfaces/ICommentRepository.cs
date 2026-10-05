using Zpravodajstvi.Domain.Entities;

namespace Zpravodajstvi.Domain.Interfaces;

public interface ICommentRepository
{
    Task<IEnumerable<Comment>> GetByArticleIdAsync(int articleId);
    Task<Comment> AddAsync(Comment comment);
    Task<Comment?> GetByIdAsync(int commentId);
    Task<Comment> UpdateAsync(Comment comment);
    Task<Comment> DeleteAsync(Comment comment);
}
