using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using Zpravodajstvi.Application.DTOs;
using Zpravodajstvi.Domain.Interfaces;
using Zpravodajstvi.Presentation.Areas.Admin.Models;

using Microsoft.AspNetCore.Authorization;
using Zpravodajstvi.Infrastructure.Data;

namespace Zpravodajstvi.Presentation.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = $"{DbInitializer.RoleAdmin},{DbInitializer.RoleRedaktor}")]
public class ArticleController : Controller
{
    private readonly IArticleService _articleService;
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly ILogger<ArticleController> _logger;

    public ArticleController(
        IArticleService articleService, 
        IWebHostEnvironment webHostEnvironment,
        ILogger<ArticleController> logger)
    {
        _articleService = articleService;
        _webHostEnvironment = webHostEnvironment;
        _logger = logger;
    }

    // Zobrazí tabulku článků
    public async Task<IActionResult> Index()
    {
        var articles = await _articleService.GetArticlesAsync();
        return View(articles);
    }

    // Zobrazí prázdný formulář s výberem kategorií
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = new ArticleCreateViewModel
        {
            Categories = await GetCategorySelectListAsync()
        };
        return View(model);
    }

    // Zpracuje odeslaný formulář
    [HttpPost]
    public async Task<IActionResult> Create(ArticleCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Pokus o vytvoření článku s nevalidními daty (Titulek: {Title}).", model.Title);
            model.Categories = await GetCategorySelectListAsync();
            return View(model); // Pokud uživatel něco nevyplnil, vrátí ho zpět s chybou
        }

        try
        {
            string? imageUrl = null;

            // Nahrání souboru na disk, pokud byl vybrán
            if (model.ImageFile != null && model.ImageFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads");
                Directory.CreateDirectory(uploadsFolder);

                var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(model.ImageFile.FileName)}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await model.ImageFile.CopyToAsync(fileStream);
                }

                imageUrl = $"/uploads/{uniqueFileName}";
                _logger.LogInformation("Soubor obrázku byl úspěšně uložen do {FilePath}.", imageUrl);
            }

            var dto = new CreateArticleDto
            {
                Title = model.Title,
                Content = model.Content,
                CategoryId = model.CategoryId,
                ImageUrl = imageUrl
            };

            var created = await _articleService.CreateArticleAsync(dto);

            _logger.LogInformation("Článek '{Title}' (ID: {Id}) byl úspěšně vytvořen a uložen.", created.Title, created.Id);

            // Po úspěšném uložení přesměrujeme zpět na výpis
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chyba při vytváření článku '{Title}'.", model.Title);
            ModelState.AddModelError(string.Empty, "Při ukládání článku došlo k neočekávané chybě.");
            model.Categories = await GetCategorySelectListAsync();
            return View(model);
        }
    }

    // Zobrazí formulář pro úpravu existujícího článku
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var article = await _articleService.GetArticleDetailsAsync(id);
        if (article == null)
        {
            return NotFound();
        }

        var model = new ArticleEditViewModel
        {
            Id = article.Id,
            Title = article.Title,
            Content = article.Content,
            CategoryId = article.CategoryId,
            ExistingImageUrl = article.Images?.FirstOrDefault()?.Url,
            Categories = await GetCategorySelectListAsync()
        };

        return View(model);
    }

    // Zpracuje odeslané úpravy článku
    [HttpPost]
    public async Task<IActionResult> Edit(ArticleEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Pokus o úpravu článku {Id} s nevalidními daty.", model.Id);
            model.Categories = await GetCategorySelectListAsync();
            return View(model);
        }

        try
        {
            string? imageUrl = null;

            if (model.ImageFile != null && model.ImageFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads");
                Directory.CreateDirectory(uploadsFolder);

                var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(model.ImageFile.FileName)}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await model.ImageFile.CopyToAsync(fileStream);
                }

                imageUrl = $"/uploads/{uniqueFileName}";
                _logger.LogInformation("Nová náhledová fotka pro článek {Id} uložena do {FilePath}.", model.Id, imageUrl);
            }

            await _articleService.UpdateArticleAsync(model.Id, model.Title, model.Content, model.CategoryId, imageUrl);
            _logger.LogInformation("Článek ID {Id} byl úspěšně aktualizován.", model.Id);

            return RedirectToAction(nameof(Index));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chyba při úpravě článku {Id}.", model.Id);
            ModelState.AddModelError(string.Empty, "Při úpravě článku došlo k neočekávané chybě.");
            model.Categories = await GetCategorySelectListAsync();
            return View(model);
        }
    }

    // Zobrazí potvrzovací obrazovku pro smazání článku
    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var article = await _articleService.GetArticleDetailsAsync(id);
        if (article == null)
        {
            return NotFound();
        }

        return View(article);
    }

    // Zpracuje finální požadavek na smazání článku
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        await _articleService.DeleteArticleAsync(id);
        _logger.LogInformation("Článek ID {Id} byl trvale smazán.", id);
        return RedirectToAction(nameof(Index));
    }

    private async Task<IEnumerable<SelectListItem>> GetCategorySelectListAsync()
    {
        var categories = await _articleService.GetCategoriesAsync();
        return categories.Select(c => new SelectListItem
        {
            Value = c.Id.ToString(),
            Text = c.Name
        }).ToList();
    }
}
