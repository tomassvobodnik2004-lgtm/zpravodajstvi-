using Microsoft.AspNetCore.Identity;

namespace Zpravodajstvi.Domain.Entities;

// Uživatel aplikace (ASP.NET Core Identity). Je v doméně, aby na něj články a komentáře
// mohly mít navigační vlastnost (Article.Author, Comment.User).
public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Jméno pro zobrazení u článků a komentářů: celé jméno, případně e-mail
    public string DisplayName => !string.IsNullOrWhiteSpace(FullName) ? FullName : (Email ?? UserName ?? string.Empty);

    // Navigační vlastnosti: články, které uživatel napsal, a jeho komentáře
    public ICollection<Article> Articles { get; set; } = new List<Article>();
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
}
