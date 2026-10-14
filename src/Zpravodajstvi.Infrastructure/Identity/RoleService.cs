using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Zpravodajstvi.Application.DTOs;
using Zpravodajstvi.Application.Interfaces;
using Zpravodajstvi.Domain.Constants;
using Zpravodajstvi.Domain.Entities;

namespace Zpravodajstvi.Infrastructure.Identity;

// Správa rolí přes RoleManager. Systémové role (Admin, Redaktor, Ctenar) používá kód v [Authorize],
// proto je nejde přejmenovat ani smazat.
public class RoleService : IRoleService
{
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;

    public RoleService(RoleManager<IdentityRole> roleManager, UserManager<ApplicationUser> userManager)
    {
        _roleManager = roleManager;
        _userManager = userManager;
    }

    public async Task<IEnumerable<RoleDto>> GetAllRolesAsync()
    {
        var roles = await _roleManager.Roles
            .OrderBy(r => r.Name)
            .ToListAsync();

        var result = new List<RoleDto>();
        foreach (var role in roles)
        {
            result.Add(await ToDtoAsync(role));
        }

        return result;
    }

    public async Task<RoleDto?> GetRoleByIdAsync(string id)
    {
        var role = await _roleManager.FindByIdAsync(id);
        return role == null ? null : await ToDtoAsync(role);
    }

    public async Task<OperationResult> CreateRoleAsync(string name)
    {
        name = name.Trim();
        if (await _roleManager.RoleExistsAsync(name))
        {
            return OperationResult.Failure($"Role {name} již existuje.");
        }

        var result = await _roleManager.CreateAsync(new IdentityRole(name));
        return ToOperationResult(result);
    }

    public async Task<OperationResult> RenameRoleAsync(string id, string newName)
    {
        var role = await _roleManager.FindByIdAsync(id);
        if (role == null)
        {
            throw new KeyNotFoundException($"Role s ID {id} nebyla nalezena.");
        }

        if (IsSystemRole(role.Name))
        {
            return OperationResult.Failure($"Systémovou roli {role.Name} nelze přejmenovat.");
        }

        newName = newName.Trim();
        var existing = await _roleManager.FindByNameAsync(newName);
        if (existing != null && existing.Id != role.Id)
        {
            return OperationResult.Failure($"Role {newName} již existuje.");
        }

        await _roleManager.SetRoleNameAsync(role, newName);
        var result = await _roleManager.UpdateAsync(role);
        return ToOperationResult(result);
    }

    public async Task<OperationResult> DeleteRoleAsync(string id)
    {
        var role = await _roleManager.FindByIdAsync(id);
        if (role == null)
        {
            throw new KeyNotFoundException($"Role s ID {id} nebyla nalezena.");
        }

        if (IsSystemRole(role.Name))
        {
            return OperationResult.Failure($"Systémovou roli {role.Name} nelze smazat.");
        }

        var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name!);
        if (usersInRole.Count > 0)
        {
            return OperationResult.Failure($"Roli {role.Name} nelze smazat, má ji přiřazenou {usersInRole.Count} uživatel(ů). Nejprve jim změňte roli.");
        }

        var result = await _roleManager.DeleteAsync(role);
        return ToOperationResult(result);
    }

    private static bool IsSystemRole(string? roleName)
    {
        return AppRoles.SystemRoles.Contains(roleName);
    }

    private async Task<RoleDto> ToDtoAsync(IdentityRole role)
    {
        var users = await _userManager.GetUsersInRoleAsync(role.Name!);
        return new RoleDto
        {
            Id = role.Id,
            Name = role.Name ?? string.Empty,
            UsersCount = users.Count,
            IsSystemRole = IsSystemRole(role.Name)
        };
    }

    private static OperationResult ToOperationResult(IdentityResult result)
    {
        return result.Succeeded
            ? OperationResult.Success()
            : OperationResult.Failure(result.Errors.Select(e => e.Description));
    }
}
