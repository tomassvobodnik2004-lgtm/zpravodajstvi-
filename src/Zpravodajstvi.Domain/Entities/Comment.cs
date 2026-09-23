namespace Zpravodajstvi.Domain.Entities;

public class Comment
{
    public int Id { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Cizí klíč a navigační vlastnost pro článek
    public int ArticleId { get; set; }
    public Article Article { get; set; } = null!;
}
