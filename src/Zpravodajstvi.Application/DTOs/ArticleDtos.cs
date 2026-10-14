using Microsoft.AspNetCore.Http;

namespace Zpravodajstvi.Application.DTOs;

public class ArticleListDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Perex { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    // Jméno autora článku (z navigační vlastnosti Article.Author)
    public string AuthorName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string? ImageUrl { get; set; }
    public int CommentsCount { get; set; }
    public List<string> Tags { get; set; } = new();
}

public class ArticleDetailDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Perex { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string? AuthorId { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<string> Tags { get; set; } = new();
    // Id štítků článku – předvyplnění výběru štítků ve formuláři úprav
    public List<int> TagIds { get; set; } = new();
    public List<ImageDto> Images { get; set; } = new();
    public List<CommentDto> Comments { get; set; } = new();

    public CreateCommentDto NewComment { get; set; } = new();
    // Úprava komentáře, která neprošla validací – view u ní znovu otevře formulář s chybou
    public UpdateComment? EditComment { get; set; }
    // Jméno přihlášeného uživatele (jen pro zobrazení ve formuláři komentáře, autora určuje server)
    public string? CurrentUserName { get; set; }
}

public class ImageDto
{
    public int Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public string Caption { get; set; } = string.Empty;
}

public class CategoryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int ArticlesCount { get; set; }
}

public class TagDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int ArticlesCount { get; set; }
}

// Data nového článku. Autora sem controller nedává – službě ho předá zvlášť podle přihlášeného uživatele.
public class CreateArticleDto
{
    public string Title { get; set; } = string.Empty;
    public string Perex { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public List<int> TagIds { get; set; } = new();
    // Nahraný obrázek (nepovinný) – uloží ho IFileStorageService
    public IFormFile? Image { get; set; }
}

public class UpdateArticleDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public List<int> TagIds { get; set; } = new();
    // Nový obrázek nahradí dosavadní obrázek článku; null = obrázek se nemění
    public IFormFile? Image { get; set; }
}
