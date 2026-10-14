using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zpravodajstvi.Application.Interfaces;
using Zpravodajstvi.Domain.Constants;
using Zpravodajstvi.Presentation.Areas.Admin.Models;

namespace Zpravodajstvi.Presentation.Areas.Admin.Controllers;

// Správa štítků (#hashtagů) – Admin a Redaktor. Štítky se k článkům přiřazují ve formuláři článku.
[Area("Admin")]
[Authorize(Roles = AppRoles.ContentManagers)]
public class TagController : Controller
{
    private readonly ITagService _tagService;
    private readonly ILogger<TagController> _logger;

    public TagController(ITagService tagService, ILogger<TagController> logger)
    {
        _tagService = tagService;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var tags = await _tagService.GetAllTagsAsync();
        return View(tags);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new TagViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TagViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var created = await _tagService.CreateTagAsync(model.Name);
        _logger.LogInformation("Štítek '{Name}' (ID: {Id}) byl vytvořen.", created.Name, created.Id);
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var tag = await _tagService.GetTagByIdAsync(id);
        if (tag == null)
        {
            return NotFound();
        }

        return View(new TagViewModel { Id = tag.Id, Name = tag.Name });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(TagViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            await _tagService.UpdateTagAsync(model.Id, model.Name);
            _logger.LogInformation("Štítek ID {Id} byl přejmenován na '{Name}'.", model.Id, model.Name);
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
        var tag = await _tagService.GetTagByIdAsync(id);
        if (tag == null)
        {
            return NotFound();
        }

        return View(tag);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        await _tagService.DeleteTagAsync(id);
        _logger.LogInformation("Štítek ID {Id} byl smazán.", id);
        return RedirectToAction(nameof(Index));
    }
}
