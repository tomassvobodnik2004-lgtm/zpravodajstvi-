namespace Zpravodajstvi.Domain.Entities;

public class Image
{
    public int Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public string Caption { get; set; } = string.Empty;

    // Cizí klíč a navigační vlastnost pro článek
    public int ArticleId { get; set; }
    public Article Article { get; set; } = null!;
}
