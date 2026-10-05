using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Zpravodajstvi.Domain.Interfaces;
using Zpravodajstvi.Application.Interfaces;
using Zpravodajstvi.Infrastructure.Identity;
using Zpravodajstvi.Presentation.Models;

namespace Zpravodajstvi.Presentation.Controllers;

public class HomeController : Controller
{
    private readonly IArticleService _articleService;
    private readonly UserManager<ApplicationUser> _userManager;

    public HomeController(IArticleService articleService, UserManager<ApplicationUser> userManager)
    {
        _articleService = articleService;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(int? categoryId = null, string? tag = null, string? search = null)
    {
        var articles = await _articleService.GetArticlesAsync(categoryId, tag, search);
        var categories = await _articleService.GetCategoriesAsync();

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

        // Pokud je uživatel přihlášen, předvyplníme jeho jméno do formuláře komentáře
        if (User?.Identity?.IsAuthenticated == true)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                article.NewComment.AuthorName = !string.IsNullOrWhiteSpace(user.FullName) ? user.FullName : (user.Email ?? string.Empty);
                // Předáme do view i aktuální zobrazené jméno uživatele — používané při porovnání s AuthorName u komentářů
                article.CurrentUserName = !string.IsNullOrWhiteSpace(user.FullName) ? user.FullName : (user.Email ?? string.Empty);
            }
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
