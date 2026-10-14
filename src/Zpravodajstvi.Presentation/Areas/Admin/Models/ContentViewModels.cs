using System.ComponentModel.DataAnnotations;
using Zpravodajstvi.Application.Validation;

namespace Zpravodajstvi.Presentation.Areas.Admin.Models;

// Formulář kategorie (vytvoření i úprava)
public class CategoryViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Název kategorie je povinný.")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Název kategorie musí mít 2 až 50 znaků.")]
    [Display(Name = "Název kategorie")]
    public string Name { get; set; } = string.Empty;
}

// Formulář štítku (vytvoření i úprava). Název se ukládá bez "#".
public class TagViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Název štítku je povinný.")]
    [StringLength(30, MinimumLength = 2, ErrorMessage = "Název štítku musí mít 2 až 30 znaků.")]
    [RegularExpression(@"^#?[^\s#]+$", ErrorMessage = "Štítek musí být jedno slovo bez mezer.")]
    [Display(Name = "Název štítku")]
    public string Name { get; set; } = string.Empty;
}

// Úprava komentáře v administraci. Text se validuje stejně jako u čtenářů, včetně vlastního atributu [NoProfanity].
public class CommentEditViewModel
{
    public int Id { get; set; }
    public int ArticleId { get; set; }
    public string ArticleTitle { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Obsah komentáře je povinný.")]
    [StringLength(1000, MinimumLength = 3, ErrorMessage = "Komentář musí mít mezi 3 a 1000 znaky.")]
    [NoProfanity(ErrorMessage = "Komentář obsahuje nepovolená nebo vulgární slova.")]
    [Display(Name = "Text komentáře")]
    public string Content { get; set; } = string.Empty;
}
