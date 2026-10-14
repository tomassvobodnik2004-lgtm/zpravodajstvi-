using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zpravodajstvi.Application.DTOs;
using Zpravodajstvi.Application.Interfaces;
using Zpravodajstvi.Domain.Constants;

namespace Zpravodajstvi.Presentation.Controllers;

public class CommentsController : Controller
{
    private readonly ICommentService _commentService;
    private readonly IArticleService _articleService;
    private readonly IUserService _userService;
    private readonly ILogger<CommentsController> _logger;

    public CommentsController(
        ICommentService commentService,
        IArticleService articleService,
        IUserService userService,
        ILogger<CommentsController> logger)
    {
        _commentService = commentService;
        _articleService = articleService;
        _userService = userService;
        _logger = logger;
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.All)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add([Bind(Prefix = "NewComment")] CreateCommentDto model)
    {
        // Autora vždy určuje server podle přihlášeného účtu (Id z claimu, jméno dohledá služba), z formuláře nic nepřichází
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            return Challenge();
        }

        if (!ModelState.IsValid)
        {
            var article = await _articleService.GetArticleDetailsAsync(model.ArticleId);
            if (article == null)
            {
                return NotFound();
            }

            article.NewComment = model;
            article.CurrentUserName = await _userService.GetDisplayNameAsync(userId);
            return View("~/Views/Home/Details.cshtml", article);
        }

        try
        {
            var created = await _commentService.AddCommentAsync(model, userId);
            _logger.LogInformation("Uživatel {UserId} přidal komentář {CommentId} k článku {ArticleId}.", userId, created.Id, model.ArticleId);
            TempData["SuccessMessage"] = "Komentář byl úspěšně přidán.";
            return RedirectToAction("Details", "Home", new { id = model.ArticleId });
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (UnauthorizedAccessException)
        {
            return Challenge();
        }
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit([Bind(Prefix = "EditComment")] UpdateComment model)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // Stejně jako při přidání: nevalidní text (např. vulgární slovo) vrátí detail článku s chybovou hláškou
        if (!ModelState.IsValid)
        {
            var article = await _articleService.GetArticleDetailsAsync(model.ArticleId);
            if (article == null)
            {
                return NotFound();
            }

            article.EditComment = model;
            article.CurrentUserName = userId != null ? await _userService.GetDisplayNameAsync(userId) : null;
            return View("~/Views/Home/Details.cshtml", article);
        }

        try
        {
            // Vlastník komentáře se pozná podle Id přihlášeného uživatele, moderátor (Admin, Redaktor) smí upravit jakýkoli
            await _commentService.UpdateCommentAsync(model.Id, model.Content, userId, IsModerator());
            _logger.LogInformation("Uživatel {UserId} upravil komentář {CommentId}.", userId, model.Id);
            TempData["SuccessMessage"] = "Komentář byl úspěšně upraven.";
            return RedirectToAction("Details", "Home", new { id = model.ArticleId }, $"comment-{model.Id}");
        }
        catch (UnauthorizedAccessException)
        {
            _logger.LogWarning("Uživatel {UserId} se pokusil upravit cizí komentář {CommentId}.", userId, model.Id);
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

        try
        {
            await _commentService.DeleteCommentAsync(id, userId, IsModerator());
            _logger.LogInformation("Uživatel {UserId} smazal komentář {CommentId}.", userId, id);
            TempData["SuccessMessage"] = "Komentář byl smazán.";
            return RedirectToAction("Details", "Home", new { id = articleId });
        }
        catch (UnauthorizedAccessException)
        {
            _logger.LogWarning("Uživatel {UserId} se pokusil smazat cizí komentář {CommentId}.", userId, id);
            return Forbid();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    // Komentáře moderuje (upravuje a maže cizí) Admin a Redaktor
    private bool IsModerator()
    {
        return User.IsInRole(AppRoles.Admin) || User.IsInRole(AppRoles.Redaktor);
    }
}
