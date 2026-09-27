using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Zpravodajstvi.Presentation.Areas.Admin.Models;

public class UserViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string CurrentRole { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class EditUserViewModel
{
    public string UserId { get; set; } = string.Empty;

    [Required(ErrorMessage = "E-mail je povinný.")]
    [EmailAddress(ErrorMessage = "Zadejte platný e-mail.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Jméno je povinné.")]
    public string FullName { get; set; } = string.Empty;

    public string SelectedRole { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Display(Name = "Nové heslo")]
    public string? NewPassword { get; set; }

    public List<SelectListItem> AvailableRoles { get; set; } = new();
}

public class EditUserRoleViewModel : EditUserViewModel
{
}
