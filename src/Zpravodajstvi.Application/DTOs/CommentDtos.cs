using System.ComponentModel.DataAnnotations;
using Zpravodajstvi.Application.Validation;

namespace Zpravodajstvi.Application.DTOs;

// Data z formuláře pro přidání komentáře. Jméno autora formulář neposílá –
// server ho vždy bere z přihlášeného účtu (CommentsController.Add).
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
    public string AuthorName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int ArticleId { get; set; }
    // Id autora (Comment.UserId) – podle něj view pozná, zda může přihlášený uživatel komentář upravit
    public string? UserId { get; set; }
}
