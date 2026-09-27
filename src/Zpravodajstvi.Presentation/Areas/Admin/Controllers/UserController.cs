using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Zpravodajstvi.Infrastructure.Data;
using Zpravodajstvi.Infrastructure.Identity;
using Zpravodajstvi.Presentation.Areas.Admin.Models;

namespace Zpravodajstvi.Presentation.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = DbInitializer.RoleAdmin)]
public class UserController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public UserController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<IActionResult> Index()
    {
        var users = await _userManager.Users.ToListAsync();
        var userViewModels = new List<UserViewModel>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            userViewModels.Add(new UserViewModel
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                FullName = user.FullName,
                CurrentRole = roles.FirstOrDefault() ?? "Bez role",
                CreatedAt = user.CreatedAt
            });
        }

        return View(userViewModels);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        var roles = await _userManager.GetRolesAsync(user);
        var currentRole = roles.FirstOrDefault() ?? DbInitializer.RoleCtenar;
        var allRoles = await _roleManager.Roles.Select(r => r.Name!).ToListAsync();

        var model = new EditUserViewModel
        {
            UserId = user.Id,
            Email = user.Email ?? string.Empty,
            FullName = user.FullName,
            SelectedRole = currentRole,
            AvailableRoles = allRoles.Select(r => new SelectListItem
            {
                Value = r,
                Text = r
            }).ToList()
        };

        return View(model);
    }

    [HttpGet]
    public Task<IActionResult> EditRole(string id) => Edit(id);

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditUserViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var allRoles = await _roleManager.Roles.Select(r => r.Name!).ToListAsync();
            model.AvailableRoles = allRoles.Select(r => new SelectListItem
            {
                Value = r,
                Text = r
            }).ToList();
            return View(model);
        }

        var user = await _userManager.FindByIdAsync(model.UserId);
        if (user == null)
        {
            return NotFound();
        }

        user.FullName = model.FullName;

        if (!string.Equals(user.Email, model.Email, StringComparison.OrdinalIgnoreCase))
        {
            var emailExists = await _userManager.FindByEmailAsync(model.Email);
            if (emailExists != null && emailExists.Id != user.Id)
            {
                ModelState.AddModelError("Email", "Tento e-mail již používá jiný uživatel.");
                var allRoles = await _roleManager.Roles.Select(r => r.Name!).ToListAsync();
                model.AvailableRoles = allRoles.Select(r => new SelectListItem
                {
                    Value = r,
                    Text = r
                }).ToList();
                return View(model);
            }

            await _userManager.SetEmailAsync(user, model.Email);
            await _userManager.SetUserNameAsync(user, model.Email);
        }

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            foreach (var error in updateResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            var allRoles = await _roleManager.Roles.Select(r => r.Name!).ToListAsync();
            model.AvailableRoles = allRoles.Select(r => new SelectListItem
            {
                Value = r,
                Text = r
            }).ToList();
            return View(model);
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        await _userManager.RemoveFromRolesAsync(user, currentRoles);

        if (!string.IsNullOrEmpty(model.SelectedRole) && await _roleManager.RoleExistsAsync(model.SelectedRole))
        {
            await _userManager.AddToRoleAsync(user, model.SelectedRole);
        }

        if (!string.IsNullOrWhiteSpace(model.NewPassword))
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetResult = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);
            if (!resetResult.Succeeded)
            {
                foreach (var error in resetResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                var allRoles = await _roleManager.Roles.Select(r => r.Name!).ToListAsync();
                model.AvailableRoles = allRoles.Select(r => new SelectListItem
                {
                    Value = r,
                    Text = r
                }).ToList();
                return View(model);
            }
        }

        TempData["SuccessMessage"] = $"Účet uživatele {user.Email} byl úspěšně aktualizován.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> EditRole(EditUserViewModel model) => Edit(model);

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user != null)
        {
            await _userManager.DeleteAsync(user);
            TempData["SuccessMessage"] = $"Uživatel {user.Email} byl úspěšně smazán.";
        }

        return RedirectToAction(nameof(Index));
    }
}
