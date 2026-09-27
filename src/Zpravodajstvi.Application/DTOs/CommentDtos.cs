using System.ComponentModel.DataAnnotations;
using Zpravodajstvi.Application.Validation;

namespace Zpravodajstvi.Application.DTOs;

public class CreateCommentDto
{
    public int ArticleId { get; set; }

    [Required(ErrorMessage = "Jméno je povinné.")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Jméno musí mít 2 až 50 znaků.")]
    [NoProfanity(ErrorMessage = "Jméno autora obsahuje nepovolená slova.")]
    [Display(Name = "Vaše jméno")]
    public string AuthorName { get; set; } = string.Empty;

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
}
