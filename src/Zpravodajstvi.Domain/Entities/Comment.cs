namespace Zpravodajstvi.Domain.Entities;

public class Comment
{
    public int Id { get; set; }
    // Jméno autora v okamžiku napsání komentáře. Zobrazí se, jen když komentář nemá uživatele (User == null).
    public string AuthorName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Cizí klíč a navigační vlastnost pro autora (uživatel Identity) – podle UserId se pozná vlastník komentáře.
    // Null = autora nelze určit, takový komentář může upravit nebo smazat jen administrátor nebo redaktor.
    public string? UserId { get; set; }
    public ApplicationUser? User { get; set; }

    // Cizí klíč a navigační vlastnost pro článek
    public int ArticleId { get; set; }
    public Article Article { get; set; } = null!;
}
