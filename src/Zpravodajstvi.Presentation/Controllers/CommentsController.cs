using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Zpravodajstvi.Application.DTOs;
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
        // Pokud je uživatel přihlášen a jméno nezadal nebo chce použít své systémové, doplníme ho
        if (User?.Identity?.IsAuthenticated == true)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null && string.IsNullOrWhiteSpace(model.AuthorName))
            {
                model.AuthorName = !string.IsNullOrWhiteSpace(user.FullName) ? user.FullName : (user.Email ?? "Čtenář");
            }
        }

        if (!string.IsNullOrWhiteSpace(model.AuthorName))
        {
            ModelState.Remove("AuthorName");
            ModelState.Remove("NewComment.AuthorName");
        }

        if (!ModelState.IsValid)
        {
            var article = await _articleService.GetArticleDetailsAsync(model.ArticleId);
            if (article == null)
            {
                return NotFound();
            }

            article.NewComment = model;
            return View("~/Views/Home/Details.cshtml", article);
        }

        try
        {
            await _commentService.AddCommentAsync(model);
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
    public async Task<IActionResult> Edit(int id, int articleId, string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return RedirectToAction("Details", "Home", new { id = articleId });
        }

        var userName = User.Identity?.Name ?? string.Empty;
        var isAdmin = User.IsInRole("Admin") || User.IsInRole(DbInitializer.RoleAdmin);

        try
        {
            await _commentService.UpdateCommentAsync(id, content, userName, isAdmin);
            TempData["SuccessMessage"] = "Komentář byl upraven.";
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

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, int articleId)
    {
        var userName = User.Identity?.Name ?? string.Empty;
        var isAdmin = User.IsInRole("Admin") || User.IsInRole(DbInitializer.RoleAdmin);

        try
        {
            await _commentService.DeleteCommentAsync(id, userName, isAdmin);
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
}

