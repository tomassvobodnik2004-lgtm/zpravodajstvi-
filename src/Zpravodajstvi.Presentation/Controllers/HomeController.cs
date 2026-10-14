using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Zpravodajstvi.Application.Interfaces;
using Zpravodajstvi.Presentation.Models;

namespace Zpravodajstvi.Presentation.Controllers;

public class HomeController : Controller
{
    private readonly IArticleService _articleService;
    private readonly ICategoryService _categoryService;
    private readonly IUserService _userService;

    public HomeController(IArticleService articleService, ICategoryService categoryService, IUserService userService)
    {
        _articleService = articleService;
        _categoryService = categoryService;
        _userService = userService;
    }

    public async Task<IActionResult> Index(int? categoryId = null, string? tag = null, string? search = null)
    {
        var articles = await _articleService.GetArticlesAsync(categoryId, tag, search);
        var categories = await _categoryService.GetAllCategoriesAsync();

        ViewBag.Categories = categories;
        ViewBag.CurrentCategory = categoryId;
        ViewBag.CurrentTag = tag;
        ViewBag.CurrentSearch = search;

        return View(articles);
    }

    public async Task<IActionResult> Details(int id)
    {
        var article = await _articleService.GetArticleDetailsAsync(id);
        if (article == null)
        {
            return NotFound();
        }

        // Pokud je uživatel přihlášen, zobrazíme jeho jméno ve formuláři komentáře
        // (jen pro informaci – autora komentáře při uložení určuje server podle účtu)
        var userId = User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId != null)
        {
            article.CurrentUserName = await _userService.GetDisplayNameAsync(userId);
        }

        return View(article);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
