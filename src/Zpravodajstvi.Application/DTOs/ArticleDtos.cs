namespace Zpravodajstvi.Application.DTOs;

public class ArticleListDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Perex { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public int CategoryId { get; set; }
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
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<string> Tags { get; set; } = new();
    public List<ImageDto> Images { get; set; } = new();
    public List<CommentDto> Comments { get; set; } = new();

    public CreateCommentDto NewComment { get; set; } = new();
    // Jméno přihlášeného uživatele (pomocné pole pro zobrazení tlačítek u komentářů)
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

public class CreateArticleDto
{
    public string Title { get; set; } = string.Empty;
    public string Perex { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string? ImageUrl { get; set; }
    public string? ImageCaption { get; set; }
}

