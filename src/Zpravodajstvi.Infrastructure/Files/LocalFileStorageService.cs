using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Zpravodajstvi.Application.Interfaces;
using Zpravodajstvi.Application.Validation;

namespace Zpravodajstvi.Infrastructure.Files;

// Ukládá nahrané obrázky do složky wwwroot/uploads, odkud je web vrací jako statické soubory
public class LocalFileStorageService : IFileStorageService
{
    private const string UploadsFolder = "uploads";

    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<LocalFileStorageService> _logger;

    public LocalFileStorageService(IWebHostEnvironment environment, ILogger<LocalFileStorageService> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    public async Task<string> SaveImageAsync(IFormFile file)
    {
        if (!ImageFileAttribute.IsAllowedImage(file, out var error))
        {
            throw new InvalidOperationException(error);
        }

        var uploadsPath = Path.Combine(_environment.WebRootPath, UploadsFolder);
        Directory.CreateDirectory(uploadsPath);

        // Guid v názvu zabrání přepsání souboru se stejným jménem
        var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
        var filePath = Path.Combine(uploadsPath, uniqueFileName);

        using (var fileStream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(fileStream);
        }

        var url = $"/{UploadsFolder}/{uniqueFileName}";
        _logger.LogInformation("Soubor obrázku byl uložen do {FilePath}.", url);
        return url;
    }

    public void DeleteImage(string url)
    {
        // Mažou se jen soubory nahrané do /uploads/, externí obrázky (https://...) zůstanou
        if (string.IsNullOrEmpty(url) || !url.StartsWith($"/{UploadsFolder}/", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var filePath = Path.Combine(_environment.WebRootPath, UploadsFolder, Path.GetFileName(url));
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            _logger.LogInformation("Soubor obrázku {FilePath} byl smazán.", url);
        }
    }
}
