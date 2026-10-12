using System.ComponentModel.DataAnnotations;
using Zpravodajstvi.Application.Validation;


namespace Zpravodajstvi.Application.DTOs
{
    // Data z formuláře pro úpravu komentáře – validují se stejně jako při vytvoření (CreateCommentDto)
    public class UpdateComment
    {
        public int Id { get; set; }
        public int ArticleId { get; set; }

        [Required(ErrorMessage = "Obsah komentáře je povinný.")]
        [StringLength(1000, MinimumLength = 3, ErrorMessage = "Komentář musí mít mezi 3 a 1000 znaky.")]
        [NoProfanity(ErrorMessage = "Komentář obsahuje nepovolená nebo vulgární slova.")]
        public string Content { get; set; } = string.Empty;
    }
}
