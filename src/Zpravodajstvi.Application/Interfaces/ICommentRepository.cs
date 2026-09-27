using Zpravodajstvi.Domain.Entities;

namespace Zpravodajstvi.Application.Interfaces;

public interface ICommentRepository
{
    Task<IEnumerable<Comment>> GetByArticleIdAsync(int articleId);
    Task<Comment> AddAsync(Comment comment);
}
