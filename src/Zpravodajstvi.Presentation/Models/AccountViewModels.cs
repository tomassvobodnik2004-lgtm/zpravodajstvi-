using System.ComponentModel.DataAnnotations;

namespace Zpravodajstvi.Presentation.Models;

public class RegisterViewModel
{
    [Required(ErrorMessage = "Jméno a příjmení je povinné.")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Jméno musí mít 3 až 100 znaků.")]
    [Display(Name = "Celé jméno")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "E-mail je povinný.")]
    [EmailAddress(ErrorMessage = "Zadejte platnou e-mailovou adresu.")]
    [Display(Name = "E-mail")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Heslo je povinné.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Heslo musí mít alespoň {2} znaků.")]
    [DataType(DataType.Password)]
    [Display(Name = "Heslo")]
    public string Password { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Display(Name = "Potvrzení hesla")]
    [Compare("Password", ErrorMessage = "Zadaná hesla se neshodují.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class LoginViewModel
{
    [Required(ErrorMessage = "E-mail je povinný.")]
    [EmailAddress(ErrorMessage = "Zadejte platný formát e-mailu.")]
    [Display(Name = "E-mail")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Heslo je povinné.")]
    [DataType(DataType.Password)]
    [Display(Name = "Heslo")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Pamatovat si přihlášení")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}
