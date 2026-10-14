using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zpravodajstvi.Application.DTOs;
using Zpravodajstvi.Application.Interfaces;
using Zpravodajstvi.Domain.Constants;
using Zpravodajstvi.Presentation.Areas.Admin.Models;

namespace Zpravodajstvi.Presentation.Areas.Admin.Controllers;

// Správa rolí – jen Admin. Systémové role (Admin, Redaktor, Ctenar) lze jen prohlížet.
[Area("Admin")]
[Authorize(Roles = AppRoles.Admin)]
public class RoleController : Controller
{
    private readonly IRoleService _roleService;
    private readonly ILogger<RoleController> _logger;

    public RoleController(IRoleService roleService, ILogger<RoleController> logger)
    {
        _roleService = roleService;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var roles = await _roleService.GetAllRolesAsync();
        return View(roles);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new RoleViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RoleViewModel model)
    {
        if (ModelState.IsValid)
        {
            var result = await _roleService.CreateRoleAsync(model.Name);
            if (result.Succeeded)
            {
                _logger.LogInformation("Byla vytvořena role {Role}.", model.Name);
                TempData["SuccessMessage"] = $"Role {model.Name} byla vytvořena.";
                return RedirectToAction(nameof(Index));
            }

            AddErrors(result);
        }

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        var role = await _roleService.GetRoleByIdAsync(id);
        if (role == null)
        {
            return NotFound();
        }

        if (role.IsSystemRole)
        {
            TempData["ErrorMessage"] = $"Systémovou roli {role.Name} nelze přejmenovat.";
            return RedirectToAction(nameof(Index));
        }

        return View(new RoleViewModel { Id = role.Id, Name = role.Name });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(RoleViewModel model)
    {
        if (ModelState.IsValid)
        {
            try
            {
                var result = await _roleService.RenameRoleAsync(model.Id, model.Name);
                if (result.Succeeded)
                {
                    _logger.LogInformation("Role {RoleId} byla přejmenována na {Role}.", model.Id, model.Name);
                    TempData["SuccessMessage"] = $"Role byla přejmenována na {model.Name}.";
                    return RedirectToAction(nameof(Index));
                }

                AddErrors(result);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Delete(string id)
    {
        var role = await _roleService.GetRoleByIdAsync(id);
        if (role == null)
        {
            return NotFound();
        }

        return View(role);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(string id)
    {
        try
        {
            var result = await _roleService.DeleteRoleAsync(id);
            if (result.Succeeded)
            {
                _logger.LogInformation("Role {RoleId} byla smazána.", id);
                TempData["SuccessMessage"] = "Role byla smazána.";
                return RedirectToAction(nameof(Index));
            }

            AddErrors(result);
            var role = await _roleService.GetRoleByIdAsync(id);
            return role == null ? NotFound() : View(role);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    private void AddErrors(OperationResult result)
    {
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error);
        }
    }
}
