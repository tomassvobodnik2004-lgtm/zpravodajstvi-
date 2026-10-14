using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Zpravodajstvi.Application.Validation;

namespace Zpravodajstvi.Presentation.Areas.Admin.Models;

// Společná pole formuláře článku (vytvoření i úprava) včetně validačních pravidel.
// Autor ve formuláři není – určuje ho server podle přihlášeného uživatele.
public abstract class ArticleFormViewModel
{
    [Required(ErrorMessage = "Titulek článku je povinný.")]
    [StringLength(200, MinimumLength = 3, ErrorMessage = "Titulek musí mít 3 až 200 znaků.")]
    [Display(Name = "Titulek článku")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Text článku je povinný.")]
    [MinLength(5, ErrorMessage = "Text článku musí mít alespoň 5 znaků.")]
    [Display(Name = "Text článku")]
    public string Content { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vyberte kategorii.")]
    [Display(Name = "Kategorie")]
    public int? CategoryId { get; set; }

    // Vlastní validační atribut: jen obrázky (.jpg, .png, .gif, .webp) do 5 MB
    [ImageFile]
    [Display(Name = "Obrázek k článku")]
    public IFormFile? ImageFile { get; set; }

    [Display(Name = "Štítky")]
    public List<int> SelectedTagIds { get; set; } = new();

    public IEnumerable<SelectListItem> Categories { get; set; } = new List<SelectListItem>();
    public IEnumerable<SelectListItem> AvailableTags { get; set; } = new List<SelectListItem>();
}

public class ArticleCreateViewModel : ArticleFormViewModel
{
    // ID, autor a datum vytvoření zde nejsou, ty doplní server
}
