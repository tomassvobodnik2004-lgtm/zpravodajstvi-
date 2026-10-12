using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zpravodajstvi.Domain.Interfaces;
using Zpravodajstvi.Application.Interfaces;
using Zpravodajstvi.Infrastructure.Data;

namespace Zpravodajstvi.Presentation.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = $"{DbInitializer.RoleAdmin},{DbInitializer.RoleRedaktor}")]
public class CategoryController : Controller
{
    private readonly ICategoryService _categoryService;

    public CategoryController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    public async Task<IActionResult> Index()
    {
        var categories = await _categoryService.GetAllCategoriesAsync();
        return View(categories);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            ModelState.AddModelError("name", "Název kategorie je povinný.");
            return View();
        }

        await _categoryService.CreateCategoryAsync(name.Trim());
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var category = await _categoryService.GetCategoryByIdAsync(id);
        if (category == null)
        {
            return NotFound();
        }

        return View(category);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        // Vazba Category -> Article má DeleteBehavior.Restrict: rubriku s články databáze smazat nedovolí
        // (SQLite Error 19), proto články zkontrolujeme předem a místo chyby 500 vrátíme srozumitelnou hlášku
        var category = await _categoryService.GetCategoryByIdAsync(id);
        if (category != null && category.ArticlesCount > 0)
        {
            ModelState.AddModelError(string.Empty, GetCategoryNotEmptyMessage(category.ArticlesCount));
            return View(category);
        }

        await _categoryService.DeleteCategoryAsync(id);
        return RedirectToAction(nameof(Index));
    }

    // "obsahuje 1 článek / 3 články / 5 článků" – české skloňování podle počtu
    private static string GetCategoryNotEmptyMessage(int articlesCount)
    {
        return articlesCount switch
        {
            1 => "Rubriku nelze smazat, obsahuje 1 článek. Nejprve ho přesuňte nebo smažte.",
            >= 2 and <= 4 => $"Rubriku nelze smazat, obsahuje {articlesCount} články. Nejprve je přesuňte nebo smažte.",
            _ => $"Rubriku nelze smazat, obsahuje {articlesCount} článků. Nejprve je přesuňte nebo smažte."
        };
    }
}
