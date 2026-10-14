using Zpravodajstvi.Application.DTOs;

namespace Zpravodajstvi.Application.Interfaces;

public interface ICommentService
{
    Task<IEnumerable<CommentDto>> GetCommentsByArticleIdAsync(int articleId);
    Task<IEnumerable<CommentDto>> GetCommentsByUserIdAsync(string userId);
    Task<IEnumerable<CommentDto>> GetAllCommentsAsync();
    Task<CommentDto?> GetCommentByIdAsync(int commentId);

    // userId = Id přihlášeného uživatele; jméno autora si služba dohledá sama
    Task<CommentDto> AddCommentAsync(CreateCommentDto dto, string userId);

    // isModerator = Admin nebo Redaktor – smí upravit a smazat jakýkoli komentář, ostatní jen svůj
    Task UpdateCommentAsync(int commentId, string content, string? currentUserId, bool isModerator);
    Task DeleteCommentAsync(int commentId, string? currentUserId, bool isModerator);
}
