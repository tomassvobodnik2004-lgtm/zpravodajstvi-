using Zpravodajstvi.Application.DTOs;

namespace Zpravodajstvi.Application.Interfaces;

public interface ICommentService
{
    Task<IEnumerable<CommentDto>> GetCommentsByArticleIdAsync(int articleId);
    Task<CommentDto> AddCommentAsync(CreateCommentDto dto, string userId, string authorName);

    Task UpdateCommentAsync(int commentId, string content, string? currentUserId, bool isAdmin);
    Task DeleteCommentAsync(int commentId, string? currentUserId, bool isAdmin);
}
