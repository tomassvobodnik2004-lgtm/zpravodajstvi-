using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Zpravodajstvi.Application.DTOs;
using Zpravodajstvi.Domain.Interfaces;
using Zpravodajstvi.Application.Interfaces;
using Zpravodajstvi.Infrastructure.Data;
using Zpravodajstvi.Infrastructure.Identity;

namespace Zpravodajstvi.Presentation.Controllers;

public class CommentsController : Controller
{
    private readonly ICommentService _commentService;
    private readonly IArticleService _articleService;
    private readonly UserManager<ApplicationUser> _userManager;

    public CommentsController(
        ICommentService _commentService,
        IArticleService articleService,
        UserManager<ApplicationUser> userManager)
    {
        this._commentService = _commentService;
        _articleService = articleService;
        _userManager = userManager;
    }

    [HttpPost]
    [Authorize(Roles = $"{DbInitializer.RoleCtenar},{DbInitializer.RoleRedaktor},{DbInitializer.RoleAdmin}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add([Bind(Prefix = "NewComment")] CreateCommentDto model)
    {
        // Autora vždy určuje server podle přihlášeného účtu (Id a jméno z databáze), jméno z formuláře se nepoužívá
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Challenge();
        }

        var authorName = GetAuthorName(user);

        if (!ModelState.IsValid)
        {
            var article = await _articleService.GetArticleDetailsAsync(model.ArticleId);
            if (article == null)
            {
                return NotFound();
            }

            article.NewComment = model;
            article.CurrentUserName = authorName;
            return View("~/Views/Home/Details.cshtml", article);
        }

        try
        {
            await _commentService.AddCommentAsync(model, user.Id, authorName);
            TempData["SuccessMessage"] = "Komentář byl úspěšně přidán.";
            return RedirectToAction("Details", "Home", new { id = model.ArticleId });
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit([Bind(Prefix = "EditComment")] UpdateComment model)
    {
        // Stejně jako při přidání: nevalidní text (např. vulgární slovo) vrátí detail článku s chybovou hláškou
        if (!ModelState.IsValid)
        {
            var article = await _articleService.GetArticleDetailsAsync(model.ArticleId);
            if (article == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            article.EditComment = model;
            article.CurrentUserName = user != null ? GetAuthorName(user) : null;
            return View("~/Views/Home/Details.cshtml", article);
        }

        var isAdmin = User.IsInRole("Admin") || User.IsInRole(DbInitializer.RoleAdmin);
        // Vlastník komentáře se pozná podle Id přihlášeného uživatele (claim NameIdentifier)
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        try
        {
            await _commentService.UpdateCommentAsync(model.Id, model.Content, userId, isAdmin);
            TempData["SuccessMessage"] = "Komentář byl úspěšně upraven.";
            return RedirectToAction("Details", "Home", new { id = model.ArticleId, fragment = $"comment-{model.Id}" });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, int articleId)
    {
        // Vlastník komentáře se pozná podle Id přihlášeného uživatele (claim NameIdentifier)
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isAdmin = User.IsInRole("Admin") || User.IsInRole(DbInitializer.RoleAdmin);

        try
        {
            await _commentService.DeleteCommentAsync(id, userId, isAdmin);
            TempData["SuccessMessage"] = "Komentář byl smazán.";
            return RedirectToAction("Details", "Home", new { id = articleId });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    // Jméno autora komentáře: celé jméno z účtu, případně e-mail
    private static string GetAuthorName(ApplicationUser user)
    {
        return !string.IsNullOrWhiteSpace(user.FullName) ? user.FullName : (user.Email ?? "Čtenář");
    }
}

