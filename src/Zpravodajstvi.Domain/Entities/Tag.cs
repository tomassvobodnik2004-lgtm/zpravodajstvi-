namespace Zpravodajstvi.Domain.Entities;

public class Tag
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    // Navigační vlastnost pro články označené tímto štítkem
    public ICollection<Article> Articles { get; set; } = new List<Article>();
}
