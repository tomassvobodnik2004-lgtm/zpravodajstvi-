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

// Vytvoření účtu administrátorem – počáteční heslo zadá admin
public class CreateUserViewModel
{
    [Required(ErrorMessage = "Jméno je povinné.")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Jméno musí mít 3 až 100 znaků.")]
    [Display(Name = "Celé jméno")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "E-mail je povinný.")]
    [EmailAddress(ErrorMessage = "Zadejte platný e-mail.")]
    [Display(Name = "E-mail")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Heslo je povinné.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Heslo musí mít alespoň {2} znaků.")]
    [DataType(DataType.Password)]
    [Display(Name = "Počáteční heslo")]
    public string Password { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Display(Name = "Potvrzení hesla")]
    [Compare(nameof(Password), ErrorMessage = "Zadaná hesla se neshodují.")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vyberte roli.")]
    [Display(Name = "Role")]
    public string SelectedRole { get; set; } = string.Empty;

    public List<SelectListItem> AvailableRoles { get; set; } = new();
}

// Úprava účtu administrátorem. Záměrně neobsahuje heslo ani PasswordHash – admin heslo uživatele měnit nemůže.
public class EditUserViewModel
{
    public string UserId { get; set; } = string.Empty;

    [Required(ErrorMessage = "E-mail je povinný.")]
    [EmailAddress(ErrorMessage = "Zadejte platný e-mail.")]
    [Display(Name = "E-mail")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Jméno je povinné.")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Jméno musí mít 3 až 100 znaků.")]
    [Display(Name = "Celé jméno")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vyberte roli.")]
    [Display(Name = "Role")]
    public string SelectedRole { get; set; } = string.Empty;

    public List<SelectListItem> AvailableRoles { get; set; } = new();
}

public class EditUserRoleViewModel : EditUserViewModel
{
}

// Formulář role (vytvoření i přejmenování)
public class RoleViewModel
{
    public string Id { get; set; } = string.Empty;

    [Required(ErrorMessage = "Název role je povinný.")]
    [StringLength(30, MinimumLength = 2, ErrorMessage = "Název role musí mít 2 až 30 znaků.")]
    [RegularExpression(@"^[A-Za-z0-9_-]+$", ErrorMessage = "Název role smí obsahovat jen písmena bez diakritiky, číslice, - a _.")]
    [Display(Name = "Název role")]
    public string Name { get; set; } = string.Empty;
}
