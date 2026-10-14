using Zpravodajstvi.Application.DTOs;
using Zpravodajstvi.Domain.Interfaces;
using Zpravodajstvi.Application.Interfaces;
using Zpravodajstvi.Domain.Entities;

namespace Zpravodajstvi.Application.Services;

public class CommentService : ICommentService
{
    private readonly ICommentRepository _commentRepository;
    private readonly IArticleRepository _articleRepository;
    private readonly IUserService _userService;

    public CommentService(ICommentRepository commentRepository, IArticleRepository articleRepository, IUserService userService)
    {
        _commentRepository = commentRepository;
        _articleRepository = articleRepository;
        _userService = userService;
    }

    public async Task<IEnumerable<CommentDto>> GetCommentsByArticleIdAsync(int articleId)
    {
        var comments = await _commentRepository.GetByArticleIdAsync(articleId);
        return comments
            .OrderByDescending(c => c.CreatedAt)
            .Select(DtoMapper.ToCommentDto);
    }

    public async Task<IEnumerable<CommentDto>> GetCommentsByUserIdAsync(string userId)
    {
        var comments = await _commentRepository.GetByUserIdAsync(userId);
        return comments.Select(DtoMapper.ToCommentDto);
    }

    public async Task<IEnumerable<CommentDto>> GetAllCommentsAsync()
    {
        var comments = await _commentRepository.GetAllWithDetailsAsync();
        return comments.Select(DtoMapper.ToCommentDto);
    }

    public async Task<CommentDto?> GetCommentByIdAsync(int commentId)
    {
        var comment = await _commentRepository.GetByIdWithDetailsAsync(commentId);
        return comment == null ? null : DtoMapper.ToCommentDto(comment);
    }

    // Autora určuje Id přihlášeného uživatele (předá ho controller z claimu), jméno si služba dohledá sama.
    // Z formuláře nepřichází ani Id, ani jméno autora.
    public async Task<CommentDto> AddCommentAsync(CreateCommentDto dto, string userId)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var articleExists = await _articleRepository.ExistsAsync(dto.ArticleId);
        if (!articleExists)
        {
            throw new KeyNotFoundException($"Článek s ID {dto.ArticleId} nebyl nalezen.");
        }

        var authorName = await _userService.GetDisplayNameAsync(userId);
        if (authorName == null)
        {
            throw new UnauthorizedAccessException("Přihlášený uživatel nebyl nalezen.");
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
        return DtoMapper.ToCommentDto(created);
    }

    public async Task UpdateCommentAsync(int commentId, string content, string? currentUserId, bool isModerator)
    {
        var comment = await _commentRepository.GetByIdAsync(commentId);
        if (comment == null)
        {
            throw new KeyNotFoundException($"Komentář s ID {commentId} nebyl nalezen.");
        }

        // Kontrola oprávnění: Admin a Redaktor (moderátoři) mohou vše, ostatní jen své vlastní komentáře (podle Id uživatele)
        if (!isModerator && !IsOwner(comment, currentUserId))
        {
            throw new UnauthorizedAccessException("Nemáte oprávnění upravovat tento komentář.");
        }

        comment.Content = content.Trim();
        await _commentRepository.UpdateAsync(comment);
    }

    public async Task DeleteCommentAsync(int commentId, string? currentUserId, bool isModerator)
    {
        var comment = await _commentRepository.GetByIdAsync(commentId);
        if (comment == null)
        {
            throw new KeyNotFoundException($"Komentář s ID {commentId} nebyl nalezen.");
        }

        // Kontrola oprávnění: Admin a Redaktor (moderátoři) mohou vše, ostatní jen své vlastní komentáře (podle Id uživatele)
        if (!isModerator && !IsOwner(comment, currentUserId))
        {
            throw new UnauthorizedAccessException("Nemáte oprávnění smazat tento komentář.");
        }

        await _commentRepository.DeleteAsync(comment);
    }

    // Komentář bez UserId (autora nešlo určit) nepatří nikomu – spravuje ho jen moderátor
    private static bool IsOwner(Comment comment, string? currentUserId)
    {
        return !string.IsNullOrEmpty(comment.UserId)
            && string.Equals(comment.UserId, currentUserId, StringComparison.Ordinal);
    }
}
