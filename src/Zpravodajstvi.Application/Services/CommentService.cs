using Zpravodajstvi.Application.DTOs;
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
                ArticleId = c.ArticleId
            });
    }

    public async Task<CommentDto> AddCommentAsync(CreateCommentDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var articleExists = await _articleRepository.ExistsAsync(dto.ArticleId);
        if (!articleExists)
        {
            throw new KeyNotFoundException($"Článek s ID {dto.ArticleId} nebyl nalezen.");
        }

        var comment = new Comment
        {
            AuthorName = dto.AuthorName.Trim(),
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
            ArticleId = created.ArticleId
        };
    }
}
