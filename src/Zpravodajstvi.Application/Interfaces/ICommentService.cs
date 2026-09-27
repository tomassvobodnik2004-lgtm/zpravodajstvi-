using Zpravodajstvi.Application.DTOs;

namespace Zpravodajstvi.Application.Interfaces;

public interface ICommentService
{
    Task<IEnumerable<CommentDto>> GetCommentsByArticleIdAsync(int articleId);
    Task<CommentDto> AddCommentAsync(CreateCommentDto dto);
}
