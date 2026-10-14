using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Zpravodajstvi.Application.DTOs;
using Zpravodajstvi.Application.Interfaces;
using Zpravodajstvi.Domain.Constants;
using Zpravodajstvi.Presentation.Areas.Admin.Models;

namespace Zpravodajstvi.Presentation.Areas.Admin.Controllers;

// Správa uživatelských účtů – jen Admin. S Identity pracuje přes IUserService a IRoleService.
[Area("Admin")]
[Authorize(Roles = AppRoles.Admin)]
public class UserController : Controller
{
    private readonly IUserService _userService;
    private readonly IRoleService _roleService;
    private readonly ILogger<UserController> _logger;

    public UserController(IUserService userService, IRoleService roleService, ILogger<UserController> logger)
    {
        _userService = userService;
        _roleService = roleService;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var users = await _userService.GetAllUsersAsync();
        var userViewModels = users.Select(u => new UserViewModel
        {
            Id = u.Id,
            Email = u.Email,
            FullName = u.FullName,
            CurrentRole = string.IsNullOrEmpty(u.Role) ? "Bez role" : u.Role,
            CreatedAt = u.CreatedAt
        }).ToList();

        return View(userViewModels);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = new CreateUserViewModel { SelectedRole = AppRoles.Ctenar };
        model.AvailableRoles = await GetRoleSelectListAsync();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateUserViewModel model)
    {
        if (ModelState.IsValid)
        {
            var result = await _userService.CreateUserAsync(new CreateUserDto
            {
                FullName = model.FullName,
                Email = model.Email,
                Password = model.Password,
                Role = model.SelectedRole
            });

            if (result.Succeeded)
            {
                _logger.LogInformation("Admin {AdminId} vytvořil účet {Email} s rolí {Role}.", CurrentUserId, model.Email, model.SelectedRole);
                TempData["SuccessMessage"] = $"Účet {model.Email} byl vytvořen.";
                return RedirectToAction(nameof(Index));
            }

            AddErrors(result);
        }

        model.AvailableRoles = await GetRoleSelectListAsync();
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        var user = await _userService.GetUserByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        var model = new EditUserViewModel
        {
            UserId = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            SelectedRole = string.IsNullOrEmpty(user.Role) ? AppRoles.Ctenar : user.Role,
            AvailableRoles = await GetRoleSelectListAsync()
        };

        // Explicitní název view: stejný formulář vrací i starší adresa EditRole
        return View("Edit", model);
    }

    [HttpGet]
    public Task<IActionResult> EditRole(string id) => Edit(id);

    // Úprava jména, e-mailu a role. Heslo admin měnit nemůže – formulář ani služba s ním nepracují.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditUserViewModel model)
    {
        if (ModelState.IsValid)
        {
            try
            {
                var result = await _userService.UpdateUserAsync(new UpdateUserDto
                {
                    Id = model.UserId,
                    FullName = model.FullName,
                    Email = model.Email,
                    Role = model.SelectedRole
                }, CurrentUserId ?? string.Empty);

                if (result.Succeeded)
                {
                    _logger.LogInformation("Admin {AdminId} upravil účet {UserId} (role {Role}).", CurrentUserId, model.UserId, model.SelectedRole);
                    TempData["SuccessMessage"] = $"Účet uživatele {model.Email} byl úspěšně aktualizován.";
                    return RedirectToAction(nameof(Index));
                }

                AddErrors(result);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }

        model.AvailableRoles = await GetRoleSelectListAsync();
        return View("Edit", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> EditRole(EditUserViewModel model) => Edit(model);

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        try
        {
            var user = await _userService.GetUserByIdAsync(id);
            var result = await _userService.DeleteUserAsync(id, CurrentUserId ?? string.Empty);

            if (result.Succeeded)
            {
                _logger.LogInformation("Admin {AdminId} smazal účet {Email}.", CurrentUserId, user?.Email);
                TempData["SuccessMessage"] = $"Uživatel {user?.Email} byl úspěšně smazán.";
            }
            else
            {
                TempData["ErrorMessage"] = string.Join(" ", result.Errors);
            }
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }

        return RedirectToAction(nameof(Index));
    }

    private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

    private async Task<List<SelectListItem>> GetRoleSelectListAsync()
    {
        var roles = await _roleService.GetAllRolesAsync();
        return roles.Select(r => new SelectListItem
        {
            Value = r.Name,
            Text = r.Name
        }).ToList();
    }

    private void AddErrors(OperationResult result)
    {
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error);
        }
    }
}
