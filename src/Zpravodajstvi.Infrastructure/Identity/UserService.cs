using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Zpravodajstvi.Application.DTOs;
using Zpravodajstvi.Application.Interfaces;
using Zpravodajstvi.Domain.Constants;
using Zpravodajstvi.Domain.Entities;

namespace Zpravodajstvi.Infrastructure.Identity;

// Správa uživatelských účtů přes UserManager a RoleManager.
// Úprava účtu mění jen jméno, e-mail a roli – PasswordHash se nikde nenastavuje ani nepřepisuje.
public class UserService : IUserService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public UserService(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<IEnumerable<UserDto>> GetAllUsersAsync()
    {
        var users = await _userManager.Users
            .OrderBy(u => u.CreatedAt)
            .ToListAsync();

        var result = new List<UserDto>();
        foreach (var user in users)
        {
            result.Add(await ToDtoAsync(user));
        }

        return result;
    }

    public async Task<UserDto?> GetUserByIdAsync(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        return user == null ? null : await ToDtoAsync(user);
    }

    public async Task<string?> GetDisplayNameAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        return user?.DisplayName;
    }

    public async Task<OperationResult> CreateUserAsync(CreateUserDto dto)
    {
        if (await _userManager.FindByEmailAsync(dto.Email) != null)
        {
            return OperationResult.Failure("Tento e-mail již používá jiný uživatel.");
        }

        if (!await _roleManager.RoleExistsAsync(dto.Role))
        {
            return OperationResult.Failure("Vybraná role neexistuje.");
        }

        var user = new ApplicationUser
        {
            UserName = dto.Email,
            Email = dto.Email,
            FullName = dto.FullName.Trim(),
            EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow
        };

        // Počáteční heslo nastaví admin při vytvoření účtu, UserManager z něj spočítá hash
        var result = await _userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
        {
            return OperationResult.Failure(result.Errors.Select(e => e.Description));
        }

        await _userManager.AddToRoleAsync(user, dto.Role);
        return OperationResult.Success();
    }

    public async Task<OperationResult> UpdateUserAsync(UpdateUserDto dto, string currentUserId)
    {
        var user = await _userManager.FindByIdAsync(dto.Id);
        if (user == null)
        {
            throw new KeyNotFoundException($"Uživatel s ID {dto.Id} nebyl nalezen.");
        }

        if (!await _roleManager.RoleExistsAsync(dto.Role))
        {
            return OperationResult.Failure("Vybraná role neexistuje.");
        }

        // Admin si nesmí sám odebrat roli Admin, jinak by ztratil přístup do správy účtů
        if (user.Id == currentUserId && dto.Role != AppRoles.Admin)
        {
            return OperationResult.Failure("Svému vlastnímu účtu nemůžete odebrat roli Admin.");
        }

        if (!string.Equals(user.Email, dto.Email, StringComparison.OrdinalIgnoreCase))
        {
            var emailOwner = await _userManager.FindByEmailAsync(dto.Email);
            if (emailOwner != null && emailOwner.Id != user.Id)
            {
                return OperationResult.Failure("Tento e-mail již používá jiný uživatel.");
            }

            await _userManager.SetEmailAsync(user, dto.Email);
            await _userManager.SetUserNameAsync(user, dto.Email);
        }

        user.FullName = dto.FullName.Trim();

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            return OperationResult.Failure(updateResult.Errors.Select(e => e.Description));
        }

        // Uživatel má vždy jednu roli: staré role odebereme a přidáme vybranou
        var currentRoles = await _userManager.GetRolesAsync(user);
        if (!(currentRoles.Count == 1 && currentRoles[0] == dto.Role))
        {
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            await _userManager.AddToRoleAsync(user, dto.Role);
        }

        return OperationResult.Success();
    }

    public async Task<OperationResult> DeleteUserAsync(string id, string currentUserId)
    {
        if (id == currentUserId)
        {
            return OperationResult.Failure("Nemůžete smazat svůj vlastní účet.");
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            throw new KeyNotFoundException($"Uživatel s ID {id} nebyl nalezen.");
        }

        // Články a komentáře uživatele zůstanou, cizí klíč AuthorId / UserId se nastaví na null (SetNull)
        var result = await _userManager.DeleteAsync(user);
        return result.Succeeded
            ? OperationResult.Success()
            : OperationResult.Failure(result.Errors.Select(e => e.Description));
    }

    private async Task<UserDto> ToDtoAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        return new UserDto
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            FullName = user.FullName,
            // Prázdný řetězec = uživatel nemá žádnou roli
            Role = roles.FirstOrDefault() ?? string.Empty,
            CreatedAt = user.CreatedAt
        };
    }
}
