using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zpravodajstvi.Application.Interfaces;
using Zpravodajstvi.Domain.Constants;
using Zpravodajstvi.Presentation.Areas.Admin.Models;

namespace Zpravodajstvi.Presentation.Areas.Admin.Controllers;

// Moderování komentářů – Admin a Redaktor mohou upravit nebo smazat jakýkoli komentář
[Area("Admin")]
[Authorize(Roles = AppRoles.ContentManagers)]
public class CommentController : Controller
{
    private readonly ICommentService _commentService;
    private readonly ILogger<CommentController> _logger;

    public CommentController(ICommentService commentService, ILogger<CommentController> logger)
    {
        _commentService = commentService;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var comments = await _commentService.GetAllCommentsAsync();
        return View(comments);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var comment = await _commentService.GetCommentByIdAsync(id);
        if (comment == null)
        {
            return NotFound();
        }

        return View(new CommentEditViewModel
        {
            Id = comment.Id,
            ArticleId = comment.ArticleId,
            ArticleTitle = comment.ArticleTitle,
            AuthorName = comment.AuthorName,
            Content = comment.Content
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(CommentEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            await _commentService.UpdateCommentAsync(model.Id, model.Content, CurrentUserId, isModerator: true);
            _logger.LogInformation("Moderátor {UserId} upravil komentář {CommentId}.", CurrentUserId, model.Id);
            TempData["SuccessMessage"] = "Komentář byl upraven.";
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
        var comment = await _commentService.GetCommentByIdAsync(id);
        if (comment == null)
        {
            return NotFound();
        }

        return View(comment);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        try
        {
            await _commentService.DeleteCommentAsync(id, CurrentUserId, isModerator: true);
            _logger.LogInformation("Moderátor {UserId} smazal komentář {CommentId}.", CurrentUserId, id);
            TempData["SuccessMessage"] = "Komentář byl smazán.";
            return RedirectToAction(nameof(Index));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);
}
