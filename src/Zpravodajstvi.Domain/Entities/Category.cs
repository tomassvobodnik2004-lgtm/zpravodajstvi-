namespace Zpravodajstvi.Domain.Entities;

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    // Navigační vlastnost pro články v kategorii
    public ICollection<Article> Articles { get; set; } = new List<Article>();
}
