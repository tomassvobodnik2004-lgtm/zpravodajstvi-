using Microsoft.AspNetCore.Http;

namespace Zpravodajstvi.Application.Interfaces;

// Ukládání nahraných obrázků. Implementace (Infrastructure) zapisuje do wwwroot/uploads.
public interface IFileStorageService
{
    // Uloží obrázek a vrátí jeho URL (např. "/uploads/{guid}_foto.jpg")
    Task<string> SaveImageAsync(IFormFile file);
    // Smaže dříve nahraný soubor; externí obrázky (https://...) nechá být
    void DeleteImage(string url);
}
