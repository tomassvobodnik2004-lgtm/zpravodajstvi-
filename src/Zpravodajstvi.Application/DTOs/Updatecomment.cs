using System.ComponentModel.DataAnnotations;
using Zpravodajstvi.Application.Validation;


namespace Zpravodajstvi.Application.DTOs
{
    public class Updatecomment
    {
        public int Id { get; set; }
        public int ArticleId { get; set; }

        [Required(ErrorMessage = "Obsah komentáře je povinný.")]
        [NoProfanity(ErrorMessage = "Komentář obsahuje vulgarismus.")]
        public string Content { get; set; } = string.Empty;
    }
}
