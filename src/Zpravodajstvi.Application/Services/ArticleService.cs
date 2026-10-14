using Zpravodajstvi.Application.DTOs;
using Zpravodajstvi.Domain.Interfaces;
using Zpravodajstvi.Application.Interfaces;
using Zpravodajstvi.Domain.Entities;

namespace Zpravodajstvi.Application.Services;

public class ArticleService : IArticleService
{
    private const int PerexLength = 150;

    private readonly IArticleRepository _articleRepository;
    private readonly ITagRepository _tagRepository;
    private readonly IFileStorageService _fileStorage;

    public ArticleService(IArticleRepository articleRepository, ITagRepository tagRepository, IFileStorageService fileStorage)
    {
        _articleRepository = articleRepository;
        _tagRepository = tagRepository;
        _fileStorage = fileStorage;
    }

    public async Task<IEnumerable<ArticleListDto>> GetArticlesAsync(int? categoryId = null, string? tag = null, string? search = null)
    {
        var articles = await _articleRepository.GetFilteredAsync(categoryId, tag, search);
        return articles.Select(DtoMapper.ToListDto);
    }

    public async Task<IEnumerable<ArticleListDto>> GetArticlesByAuthorAsync(string authorId)
    {
        var articles = await _articleRepository.GetFilteredAsync(authorId: authorId);
        return articles.Select(DtoMapper.ToListDto);
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
            AuthorId = article.AuthorId,
            AuthorName = article.Author?.DisplayName ?? DtoMapper.UnknownAuthor,
            CreatedAt = article.CreatedAt,
            UpdatedAt = article.UpdatedAt,
            Tags = article.Tags?.Select(t => t.Name).ToList() ?? new List<string>(),
            TagIds = article.Tags?.Select(t => t.Id).ToList() ?? new List<int>(),
            Images = article.Images?.Select(i => new ImageDto
            {
                Id = i.Id,
                Url = i.Url,
                Caption = i.Caption
            }).ToList() ?? new List<ImageDto>(),
            Comments = article.Comments?
                .OrderByDescending(c => c.CreatedAt)
                .Select(DtoMapper.ToCommentDto)
                .ToList() ?? new List<CommentDto>(),
            NewComment = new CreateCommentDto
            {
                ArticleId = article.Id
            }
        };
    }

    public async Task<ArticleDetailDto> CreateArticleAsync(CreateArticleDto dto, string authorId)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var article = new Article
        {
            Title = dto.Title,
            Perex = !string.IsNullOrWhiteSpace(dto.Perex) ? dto.Perex : CreatePerex(dto.Content),
            Content = dto.Content,
            CategoryId = dto.CategoryId,
            // Autor = přihlášený uživatel, kterého předal controller (ne hodnota z formuláře)
            AuthorId = authorId,
            CreatedAt = DateTime.UtcNow,
            Tags = await LoadTagsAsync(dto.TagIds)
        };

        // Uložení souboru obstará IFileStorageService, článek si pamatuje jen jeho URL
        if (dto.Image != null && dto.Image.Length > 0)
        {
            var imageUrl = await _fileStorage.SaveImageAsync(dto.Image);
            article.Images.Add(new Image
            {
                Url = imageUrl,
                Caption = dto.Title
            });
        }

        var createdArticle = await _articleRepository.AddAsync(article);

        return new ArticleDetailDto
        {
            Id = createdArticle.Id,
            Title = createdArticle.Title,
            Perex = createdArticle.Perex,
            Content = createdArticle.Content,
            CategoryId = createdArticle.CategoryId,
            AuthorId = createdArticle.AuthorId,
            CreatedAt = createdArticle.CreatedAt
        };
    }

    public async Task UpdateArticleAsync(UpdateArticleDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var article = await _articleRepository.GetByIdWithDetailsAsync(dto.Id);
        if (article == null)
        {
            throw new KeyNotFoundException($"Článek s ID {dto.Id} nebyl nalezen.");
        }

        article.Title = dto.Title;
        article.Content = dto.Content;
        article.Perex = CreatePerex(dto.Content);
        article.CategoryId = dto.CategoryId;
        article.UpdatedAt = DateTime.UtcNow;

        article.Tags.Clear();
        foreach (var tag in await LoadTagsAsync(dto.TagIds))
        {
            article.Tags.Add(tag);
        }

        // Nový obrázek nahradí ten dosavadní, staré soubory se smažou až po úspěšném uložení článku
        var replacedImageUrls = new List<string>();
        if (dto.Image != null && dto.Image.Length > 0)
        {
            var imageUrl = await _fileStorage.SaveImageAsync(dto.Image);
            replacedImageUrls.AddRange(article.Images.Select(i => i.Url));
            article.Images.Clear();
            article.Images.Add(new Image
            {
                Url = imageUrl,
                Caption = dto.Title,
                ArticleId = article.Id
            });
        }

        await _articleRepository.UpdateAsync(article);

        foreach (var url in replacedImageUrls)
        {
            _fileStorage.DeleteImage(url);
        }
    }

    public async Task DeleteArticleAsync(int id)
    {
        var article = await _articleRepository.GetByIdWithDetailsAsync(id);
        if (article == null)
        {
            return;
        }

        var imageUrls = article.Images.Select(i => i.Url).ToList();
        await _articleRepository.DeleteAsync(article);

        foreach (var url in imageUrls)
        {
            _fileStorage.DeleteImage(url);
        }
    }

    // Perex = prvních 150 znaků textu článku
    private static string CreatePerex(string content)
    {
        return content.Length > PerexLength ? content[..PerexLength] + "..." : content;
    }

    private async Task<List<Tag>> LoadTagsAsync(IEnumerable<int>? tagIds)
    {
        var ids = tagIds?.Distinct().ToList() ?? new List<int>();
        if (ids.Count == 0)
        {
            return new List<Tag>();
        }

        return (await _tagRepository.FindAsync(t => ids.Contains(t.Id))).ToList();
    }
}
