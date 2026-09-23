namespace Zpravodajstvi.Domain.Entities;

public class Article
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Perex { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Cizí klíč a navigační vlastnost pro kategorii
    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    // Navigační vlastnosti pro související kolekce
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public ICollection<Tag> Tags { get; set; } = new List<Tag>();
    public ICollection<Image> Images { get; set; } = new List<Image>();
}
