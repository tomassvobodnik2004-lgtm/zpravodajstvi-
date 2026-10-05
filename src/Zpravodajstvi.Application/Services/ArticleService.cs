using Zpravodajstvi.Application.DTOs;
using Zpravodajstvi.Domain.Interfaces;
using Zpravodajstvi.Application.Interfaces;
using Zpravodajstvi.Domain.Entities;

namespace Zpravodajstvi.Application.Services;

public class ArticleService : IArticleService
{
    private readonly IArticleRepository _articleRepository;

    public ArticleService(IArticleRepository articleRepository)
    {
        _articleRepository = articleRepository;
    }

    public async Task<IEnumerable<ArticleListDto>> GetArticlesAsync(int? categoryId = null, string? tag = null, string? search = null)
    {
        var articles = await _articleRepository.GetAllAsync(categoryId, tag, search);

        return articles.Select(a => new ArticleListDto
        {
            Id = a.Id,
            Title = a.Title,
            Perex = a.Perex,
            CategoryId = a.CategoryId,
            CategoryName = a.Category?.Name ?? "Bez kategorie",
            CreatedAt = a.CreatedAt,
            ImageUrl = a.Images?.FirstOrDefault()?.Url,
            CommentsCount = a.Comments?.Count ?? 0,
            Tags = a.Tags?.Select(t => t.Name).ToList() ?? new List<string>()
        });
    }

    public async Task<ArticleDetailDto?> GetArticleDetailsAsync(int id)
    {
        var article = await _articleRepository.GetByIdWithDetailsAsync(id);
        if (article == null)
        {
            return null;
        }

        return new ArticleDetailDto
        {
            Id = article.Id,
            Title = article.Title,
            Perex = article.Perex,
            Content = article.Content,
            CategoryId = article.CategoryId,
            CategoryName = article.Category?.Name ?? "Bez kategorie",
            CreatedAt = article.CreatedAt,
            UpdatedAt = article.UpdatedAt,
            Tags = article.Tags?.Select(t => t.Name).ToList() ?? new List<string>(),
            Images = article.Images?.Select(i => new ImageDto
            {
                Id = i.Id,
                Url = i.Url,
                Caption = i.Caption
            }).ToList() ?? new List<ImageDto>(),
            Comments = article.Comments?
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new CommentDto
                {
                    Id = c.Id,
                    AuthorName = c.AuthorName,
                    Content = c.Content,
                    CreatedAt = c.CreatedAt,
                    ArticleId = c.ArticleId
                }).ToList() ?? new List<CommentDto>(),
            NewComment = new CreateCommentDto
            {
                ArticleId = article.Id
            }
        };
    }

    public async Task<IEnumerable<CategoryDto>> GetCategoriesAsync()
    {
        var categories = await _articleRepository.GetCategoriesAsync();
        return categories.Select(c => new CategoryDto
        {
            Id = c.Id,
            Name = c.Name,
            ArticlesCount = c.Articles?.Count ?? 0
        });
    }

    public async Task<ArticleDetailDto> CreateArticleAsync(CreateArticleDto dto)
    {
        var perex = !string.IsNullOrWhiteSpace(dto.Perex) 
            ? dto.Perex 
            : (dto.Content.Length > 150 ? dto.Content[..150] + "..." : dto.Content);

        var article = new Article
        {
            Title = dto.Title,
            Perex = perex,
            Content = dto.Content,
            CategoryId = dto.CategoryId,
            CreatedAt = DateTime.UtcNow
        };

        if (!string.IsNullOrWhiteSpace(dto.ImageUrl))
        {
            article.Images = new List<Image>
            {
                new Image
                {
                    Url = dto.ImageUrl,
                    Caption = dto.ImageCaption ?? dto.Title
                }
            };
        }

        var createdArticle = await _articleRepository.AddAsync(article);

        return new ArticleDetailDto
        {
            Id = createdArticle.Id,
            Title = createdArticle.Title,
            Perex = createdArticle.Perex,
            Content = createdArticle.Content,
            CategoryId = createdArticle.CategoryId,
            CreatedAt = createdArticle.CreatedAt
        };
    }

    public async Task UpdateArticleAsync(int id, string title, string content, int categoryId, string? imageUrl = null)
    {
        var article = await _articleRepository.GetByIdWithDetailsAsync(id);
        if (article == null)
        {
            throw new KeyNotFoundException($"Článek s ID {id} nebyl nalezen.");
        }

        article.Title = title;
        article.Content = content;
        article.Perex = content.Length > 150 ? content[..150] + "..." : content;
        article.CategoryId = categoryId;
        article.UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(imageUrl))
        {
            if (article.Images == null)
            {
                article.Images = new List<Image>();
            }

            article.Images.Add(new Image
            {
                Url = imageUrl,
                Caption = title,
                ArticleId = article.Id
            });
        }

        await _articleRepository.UpdateAsync(article);
    }

    public async Task DeleteArticleAsync(int id)
    {
        await _articleRepository.DeleteAsync(id);
    }
}
