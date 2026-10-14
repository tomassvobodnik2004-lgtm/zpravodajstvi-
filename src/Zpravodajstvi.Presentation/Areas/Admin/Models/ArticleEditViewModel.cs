namespace Zpravodajstvi.Presentation.Areas.Admin.Models;

public class ArticleEditViewModel : ArticleFormViewModel
{
    public int Id { get; set; }
    public string? ExistingImageUrl { get; set; }
}
