using Zpravodajstvi.Application.DTOs;
using Zpravodajstvi.Domain.Interfaces;
using Zpravodajstvi.Application.Interfaces;
using Zpravodajstvi.Domain.Entities;

namespace Zpravodajstvi.Application.Services;

public class CommentService : ICommentService
{
    private readonly ICommentRepository _commentRepository;
    private readonly IArticleRepository _articleRepository;

    public CommentService(ICommentRepository commentRepository, IArticleRepository articleRepository)
    {
        _commentRepository = commentRepository;
        _articleRepository = articleRepository;
    }

    public async Task<IEnumerable<CommentDto>> GetCommentsByArticleIdAsync(int articleId)
    {
        var comments = await _commentRepository.GetByArticleIdAsync(articleId);
        return comments
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new CommentDto
            {
                Id = c.Id,
                AuthorName = c.AuthorName,
                Content = c.Content,
                CreatedAt = c.CreatedAt,
                ArticleId = c.ArticleId,
                UserId = c.UserId
            });
    }

    // Autora (Id a jméno) předává controller podle přihlášeného uživatele, nikdy ne z formuláře
    public async Task<CommentDto> AddCommentAsync(CreateCommentDto dto, string userId, string authorName)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var articleExists = await _articleRepository.ExistsAsync(dto.ArticleId);
        if (!articleExists)
        {
            throw new KeyNotFoundException($"Článek s ID {dto.ArticleId} nebyl nalezen.");
        }

        var comment = new Comment
        {
            AuthorName = authorName.Trim(),
            UserId = userId,
            Content = dto.Content.Trim(),
            CreatedAt = DateTime.UtcNow,
            ArticleId = dto.ArticleId
        };

        var created = await _commentRepository.AddAsync(comment);

        return new CommentDto
        {
            Id = created.Id,
            AuthorName = created.AuthorName,
            Content = created.Content,
            CreatedAt = created.CreatedAt,
            ArticleId = created.ArticleId,
            UserId = created.UserId
        };

    }
    public async Task UpdateCommentAsync(int commentId, string content, string? currentUserId, bool isAdmin)
    {
        var comment = await _commentRepository.GetByIdAsync(commentId);
        if (comment == null)
        {
            throw new KeyNotFoundException($"Komentář s ID {commentId} nebyl nalezen.");
        }

        // Kontrola oprávnění: Admin může vše, ostatní jen své vlastní komentáře (porovnání podle Id uživatele)
        if (!isAdmin && !IsOwner(comment, currentUserId))
        {
            throw new UnauthorizedAccessException("Nemáte oprávnění upravovat tento komentář.");
        }

        comment.Content = content.Trim();
        await _commentRepository.UpdateAsync(comment);
    }

    public async Task DeleteCommentAsync(int commentId, string? currentUserId, bool isAdmin)
    {
        var comment = await _commentRepository.GetByIdAsync(commentId);
        if (comment == null)
        {
            throw new KeyNotFoundException($"Komentář s ID {commentId} nebyl nalezen.");
        }

        // Kontrola oprávnění: Admin může vše, ostatní jen své vlastní komentáře (porovnání podle Id uživatele)
        if (!isAdmin && !IsOwner(comment, currentUserId))
        {
            throw new UnauthorizedAccessException("Nemáte oprávnění smazat tento komentář.");
        }

        await _commentRepository.DeleteAsync(comment);
    }

    // Komentář bez UserId (autora nešlo určit) nepatří nikomu – spravuje ho jen administrátor
    private static bool IsOwner(Comment comment, string? currentUserId)
    {
        return !string.IsNullOrEmpty(comment.UserId)
            && string.Equals(comment.UserId, currentUserId, StringComparison.Ordinal);
    }
}
