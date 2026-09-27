using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Zpravodajstvi.Presentation.Areas.Admin.Models;

public class ArticleEditViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int CategoryId { get; set; }

    public IFormFile? ImageFile { get; set; }
    public string? ExistingImageUrl { get; set; }

    public IEnumerable<SelectListItem> Categories { get; set; } = new List<SelectListItem>();
}
