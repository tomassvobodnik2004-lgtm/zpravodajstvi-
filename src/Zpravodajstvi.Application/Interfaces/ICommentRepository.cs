using Zpravodajstvi.Application.DTOs;
using Zpravodajstvi.Domain.Entities;

namespace Zpravodajstvi.Application.Interfaces;

public interface ICommentRepository
{
    Task<IEnumerable<Comment>> GetByArticleIdAsync(int articleId);
    Task<Comment> AddAsync(Comment comment);
    Task<Comment?> GetByIdAsync(int commentId);
    Task<Comment> UpdateAsync(Comment comment);
    Task<Comment> DeleteAsync(Comment comment);
}
