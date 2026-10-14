namespace Zpravodajstvi.Domain.Entities;

public class Article
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Perex { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Normalizované kopie textů pro vyhledávání (malá písmena, bez diakritiky).
    // Vyplňuje je ApplicationDbContext při každém uložení článku.
    public string SearchTitle { get; set; } = string.Empty;
    public string SearchPerex { get; set; } = string.Empty;
    public string SearchContent { get; set; } = string.Empty;

    // Cizí klíč a navigační vlastnost pro kategorii
    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    // Cizí klíč a navigační vlastnost pro autora (uživatel, který článek vytvořil).
    // Null = autor není známý nebo byl jeho účet smazán.
    public string? AuthorId { get; set; }
    public ApplicationUser? Author { get; set; }

    // Navigační vlastnosti pro související kolekce
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public ICollection<Tag> Tags { get; set; } = new List<Tag>();
    public ICollection<Image> Images { get; set; } = new List<Image>();
}
