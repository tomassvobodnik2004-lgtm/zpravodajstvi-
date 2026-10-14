using Zpravodajstvi.Application.DTOs;

namespace Zpravodajstvi.Presentation.Models;

// Profil přihlášeného uživatele: údaje o účtu, jeho články (Admin, Redaktor) a jeho komentáře
public class ProfileViewModel
{
    public UserDto User { get; set; } = new();
    public IEnumerable<ArticleListDto> Articles { get; set; } = new List<ArticleListDto>();
    public IEnumerable<CommentDto> Comments { get; set; } = new List<CommentDto>();
}
