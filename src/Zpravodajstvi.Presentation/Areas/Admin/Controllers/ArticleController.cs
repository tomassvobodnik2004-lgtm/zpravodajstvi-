using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Zpravodajstvi.Application.DTOs;
using Zpravodajstvi.Application.Interfaces;
using Zpravodajstvi.Domain.Constants;
using Zpravodajstvi.Presentation.Areas.Admin.Models;

namespace Zpravodajstvi.Presentation.Areas.Admin.Controllers;

// Správa článků – Admin a Redaktor (správa obsahu)
[Area("Admin")]
[Authorize(Roles = AppRoles.ContentManagers)]
public class ArticleController : Controller
{
    private readonly IArticleService _articleService;
    private readonly ICategoryService _categoryService;
    private readonly ITagService _tagService;
    private readonly ILogger<ArticleController> _logger;

    public ArticleController(
        IArticleService articleService,
        ICategoryService categoryService,
        ITagService tagService,
        ILogger<ArticleController> logger)
    {
        _articleService = articleService;
        _categoryService = categoryService;
        _tagService = tagService;
        _logger = logger;
    }

    // Zobrazí tabulku článků
    public async Task<IActionResult> Index()
    {
        var articles = await _articleService.GetArticlesAsync();
        return View(articles);
    }

    // Zobrazí prázdný formulář s výběrem kategorií a štítků
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = new ArticleCreateViewModel();
        await FillSelectListsAsync(model);
        return View(model);
    }

    // Zpracuje odeslaný formulář
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ArticleCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Pokus o vytvoření článku s nevalidními daty (Titulek: {Title}).", model.Title);
            await FillSelectListsAsync(model);
            return View(model); // Pokud uživatel něco nevyplnil, vrátí ho zpět s chybou
        }

        // Autorem článku je přihlášený uživatel (Id z claimu NameIdentifier, ne ze skrytého pole formuláře)
        var authorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (authorId == null)
        {
            return Challenge();
        }

        try
        {
            var dto = new CreateArticleDto
            {
                Title = model.Title,
                Content = model.Content,
                CategoryId = model.CategoryId.GetValueOrDefault(),
                TagIds = model.SelectedTagIds,
                Image = model.ImageFile
            };

            var created = await _articleService.CreateArticleAsync(dto, authorId);

            _logger.LogInformation("Článek '{Title}' (ID: {Id}) byl úspěšně vytvořen a uložen.", created.Title, created.Id);

            // Po úspěšném uložení přesměrujeme zpět na výpis
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chyba při vytváření článku '{Title}'.", model.Title);
            ModelState.AddModelError(string.Empty, "Při ukládání článku došlo k neočekávané chybě.");
            await FillSelectListsAsync(model);
            return View(model);
        }
    }

    // Zobrazí formulář pro úpravu existujícího článku
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var article = await _articleService.GetArticleDetailsAsync(id);
        if (article == null)
        {
            return NotFound();
        }

        var model = new ArticleEditViewModel
        {
            Id = article.Id,
            Title = article.Title,
            Content = article.Content,
            CategoryId = article.CategoryId,
            SelectedTagIds = article.TagIds,
            ExistingImageUrl = article.Images.FirstOrDefault()?.Url
        };
        await FillSelectListsAsync(model);

        return View(model);
    }

    // Zpracuje odeslané úpravy článku
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ArticleEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Pokus o úpravu článku {Id} s nevalidními daty.", model.Id);
            await FillSelectListsAsync(model);
            return View(model);
        }

        try
        {
            await _articleService.UpdateArticleAsync(new UpdateArticleDto
            {
                Id = model.Id,
                Title = model.Title,
                Content = model.Content,
                CategoryId = model.CategoryId.GetValueOrDefault(),
                TagIds = model.SelectedTagIds,
                Image = model.ImageFile
            });
            _logger.LogInformation("Článek ID {Id} byl úspěšně aktualizován.", model.Id);

            return RedirectToAction(nameof(Index));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chyba při úpravě článku {Id}.", model.Id);
            ModelState.AddModelError(string.Empty, "Při úpravě článku došlo k neočekávané chybě.");
            await FillSelectListsAsync(model);
            return View(model);
        }
    }

    // Zobrazí potvrzovací obrazovku pro smazání článku
    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var article = await _articleService.GetArticleDetailsAsync(id);
        if (article == null)
        {
            return NotFound();
        }

        return View(article);
    }

    // Zpracuje finální požadavek na smazání článku
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        await _articleService.DeleteArticleAsync(id);
        _logger.LogInformation("Článek ID {Id} byl trvale smazán.", id);
        return RedirectToAction(nameof(Index));
    }

    // Naplní výběr kategorií a štítků (zaškrtnuté jsou štítky z model.SelectedTagIds)
    private async Task FillSelectListsAsync(ArticleFormViewModel model)
    {
        var categories = await _categoryService.GetAllCategoriesAsync();
        model.Categories = categories.Select(c => new SelectListItem
        {
            Value = c.Id.ToString(),
            Text = c.Name
        }).ToList();

        var tags = await _tagService.GetAllTagsAsync();
        model.AvailableTags = tags.Select(t => new SelectListItem
        {
            Value = t.Id.ToString(),
            Text = t.Name,
            Selected = model.SelectedTagIds.Contains(t.Id)
        }).ToList();
    }
}
