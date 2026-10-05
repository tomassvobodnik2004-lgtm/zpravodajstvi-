using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zpravodajstvi.Domain.Interfaces;
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
        await _categoryService.DeleteCategoryAsync(id);
        return RedirectToAction(nameof(Index));
    }
}
