using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Zpravodajstvi.Application.Validation;

// Vlastní validační atribut pro nahrávaný obrázek: povolené jen obrázkové přípony a velikost do 5 MB.
// Brání nahrání např. .html nebo .svg souboru do wwwroot/uploads, odkud by ho server vracel jako stránku.
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false)]
public class ImageFileAttribute : ValidationAttribute
{
    public static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".gif", ".webp"];
    public const long MaxFileSizeBytes = 5 * 1024 * 1024;

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        // Obrázek je nepovinný – prázdné pole je v pořádku
        if (value is not IFormFile file || file.Length == 0)
        {
            return ValidationResult.Success;
        }

        return IsAllowedImage(file, out var error) ? ValidationResult.Success : new ValidationResult(error);
    }

    // Stejnou kontrolu volá i služba pro ukládání souborů (pro případ, že by ji někdo zavolal bez validace modelu)
    public static bool IsAllowedImage(IFormFile file, out string error)
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
        {
            error = $"Povolené jsou jen obrázky ({string.Join(", ", AllowedExtensions)}).";
            return false;
        }

        if (file.Length > MaxFileSizeBytes)
        {
            error = "Obrázek může mít nejvýše 5 MB.";
            return false;
        }

        error = string.Empty;
        return true;
    }
}
