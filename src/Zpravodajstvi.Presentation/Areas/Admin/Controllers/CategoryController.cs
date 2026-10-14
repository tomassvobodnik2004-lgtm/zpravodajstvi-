using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zpravodajstvi.Application.Interfaces;
using Zpravodajstvi.Domain.Constants;
using Zpravodajstvi.Presentation.Areas.Admin.Models;

namespace Zpravodajstvi.Presentation.Areas.Admin.Controllers;

// Správa kategorií (rubrik) – Admin a Redaktor
[Area("Admin")]
[Authorize(Roles = AppRoles.ContentManagers)]
public class CategoryController : Controller
{
    private readonly ICategoryService _categoryService;
    private readonly ILogger<CategoryController> _logger;

    public CategoryController(ICategoryService categoryService, ILogger<CategoryController> logger)
    {
        _categoryService = categoryService;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var categories = await _categoryService.GetAllCategoriesAsync();
        return View(categories);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new CategoryViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CategoryViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var created = await _categoryService.CreateCategoryAsync(model.Name);
        _logger.LogInformation("Kategorie '{Name}' (ID: {Id}) byla vytvořena.", created.Name, created.Id);
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var category = await _categoryService.GetCategoryByIdAsync(id);
        if (category == null)
        {
            return NotFound();
        }

        return View(new CategoryViewModel { Id = category.Id, Name = category.Name });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(CategoryViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            await _categoryService.UpdateCategoryAsync(model.Id, model.Name);
            _logger.LogInformation("Kategorie ID {Id} byla přejmenována na '{Name}'.", model.Id, model.Name);
            return RedirectToAction(nameof(Index));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
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
        try
        {
            await _categoryService.DeleteCategoryAsync(id);
            _logger.LogInformation("Kategorie ID {Id} byla smazána.", id);
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            // Kategorie ještě obsahuje články – místo chyby 500 vrátíme stránku mazání se srozumitelnou hláškou
            var category = await _categoryService.GetCategoryByIdAsync(id);
            if (category == null)
            {
                return NotFound();
            }

            ModelState.AddModelError(string.Empty, ex.Message);
            return View(category);
        }
    }
}
