using System.ComponentModel.DataAnnotations;
using Zpravodajstvi.Application.Validation;

namespace Zpravodajstvi.Application.DTOs;

// Data z formuláře pro přidání komentáře. Jméno autora formulář neposílá –
// autora (Id i jméno) vždy určuje server podle přihlášeného účtu (CommentService.AddCommentAsync).
public class CreateCommentDto
{
    public int ArticleId { get; set; }

    [Required(ErrorMessage = "Obsah komentáře je povinný.")]
    [StringLength(1000, MinimumLength = 3, ErrorMessage = "Komentář musí mít mezi 3 a 1000 znaky.")]
    [NoProfanity(ErrorMessage = "Komentář obsahuje nepovolená nebo vulgární slova.")]
    [Display(Name = "Obsah komentáře")]
    public string Content { get; set; } = string.Empty;
}

public class CommentDto
{
    public int Id { get; set; }
    // Jméno autora: aktuální jméno uživatele (Comment.User), u komentáře bez uživatele uložené Comment.AuthorName
    public string AuthorName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int ArticleId { get; set; }
    // Titulek článku – zobrazuje se ve správě komentářů a v profilu uživatele
    public string ArticleTitle { get; set; } = string.Empty;
    // Id autora (Comment.UserId) – podle něj view pozná, zda může přihlášený uživatel komentář upravit
    public string? UserId { get; set; }
}
