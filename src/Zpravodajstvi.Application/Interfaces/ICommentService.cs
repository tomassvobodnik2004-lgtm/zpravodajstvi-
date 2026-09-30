using Zpravodajstvi.Application.DTOs;

namespace Zpravodajstvi.Application.Interfaces;

public interface ICommentService
{
    Task<IEnumerable<CommentDto>> GetCommentsByArticleIdAsync(int articleId);
    Task<CommentDto> AddCommentAsync(CreateCommentDto dto);

    Task UpdateCommentAsync(int commentId, string content, string currentUserName, bool isAdmin);
    Task DeleteCommentAsync(int commentId, string currentUserName, bool isAdmin);
}
