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
    public async Task<IActionResult> Add(CreateCommentDto model)
    {
        // Pokud je uživatel přihlášen a jméno nezadal nebo chce použít své systémové, doplníme ho
        if (User?.Identity?.IsAuthenticated == true)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null && string.IsNullOrWhiteSpace(model.AuthorName))
            {
                model.AuthorName = !string.IsNullOrWhiteSpace(user.FullName) ? user.FullName : (user.Email ?? "Čtenář");
                ModelState.Remove(nameof(model.AuthorName));
            }
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
}
