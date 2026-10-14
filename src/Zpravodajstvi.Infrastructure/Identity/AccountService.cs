using Microsoft.AspNetCore.Identity;
using Zpravodajstvi.Application.DTOs;
using Zpravodajstvi.Application.Interfaces;
using Zpravodajstvi.Domain.Constants;
using Zpravodajstvi.Domain.Entities;

namespace Zpravodajstvi.Infrastructure.Identity;

// Registrace, přihlášení a odhlášení přes ASP.NET Core Identity (UserManager, SignInManager)
public class AccountService : IAccountService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public AccountService(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    public async Task<OperationResult> RegisterAsync(RegisterUserDto dto)
    {
        var existingUser = await _userManager.FindByEmailAsync(dto.Email);
        if (existingUser != null)
        {
            return OperationResult.Failure("Uživatel s tímto e-mailem již existuje.");
        }

        var user = new ApplicationUser
        {
            UserName = dto.Email,
            Email = dto.Email,
            FullName = dto.FullName.Trim(),
            EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
        {
            return OperationResult.Failure(result.Errors.Select(e => e.Description));
        }

        // Každý nově registrovaný uživatel automaticky získá roli Čtenář
        await _userManager.AddToRoleAsync(user, AppRoles.Ctenar);
        await _signInManager.SignInAsync(user, isPersistent: false);

        return OperationResult.Success();
    }

    public async Task<bool> LoginAsync(string email, string password, bool rememberMe)
    {
        // Uživatelské jméno je stejné jako e-mail (nastavuje se při registraci)
        var result = await _signInManager.PasswordSignInAsync(email, password, rememberMe, lockoutOnFailure: false);
        return result.Succeeded;
    }

    public async Task LogoutAsync()
    {
        await _signInManager.SignOutAsync();
    }

    public async Task<OperationResult> ChangePasswordAsync(string userId, string currentPassword, string newPassword)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return OperationResult.Failure("Uživatel nebyl nalezen.");
        }

        // UserManager ověří současné heslo, zkontroluje pravidla pro nové heslo a uloží jeho hash
        var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
        if (!result.Succeeded)
        {
            return OperationResult.Failure(result.Errors.Select(TranslateError));
        }

        // Změna hesla mění bezpečnostní razítko (SecurityStamp) – obnovíme cookie, aby uživatel zůstal přihlášený
        await _signInManager.RefreshSignInAsync(user);
        return OperationResult.Success();
    }

    // Nejčastější chybu (špatně zadané současné heslo) vrátíme česky, ostatní chyby Identity beze změny
    private static string TranslateError(IdentityError error)
    {
        return error.Code == nameof(IdentityErrorDescriber.PasswordMismatch)
            ? "Současné heslo není správné."
            : error.Description;
    }
}
