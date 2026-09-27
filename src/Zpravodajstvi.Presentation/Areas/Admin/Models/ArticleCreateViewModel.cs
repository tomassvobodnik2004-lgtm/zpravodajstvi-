using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Zpravodajstvi.Presentation.Areas.Admin.Models;

public class ArticleCreateViewModel
{
    // ID a datum vytvoření zde nejsou, ty se generují samy
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int CategoryId { get; set; }

    public IFormFile? ImageFile { get; set; }

    public IEnumerable<SelectListItem> Categories { get; set; } = new List<SelectListItem>();
}
