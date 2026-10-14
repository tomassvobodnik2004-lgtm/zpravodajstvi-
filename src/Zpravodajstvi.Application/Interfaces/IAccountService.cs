using Zpravodajstvi.Application.DTOs;

namespace Zpravodajstvi.Application.Interfaces;

// Registrace, přihlášení a odhlášení. Implementace (Infrastructure) používá UserManager a SignInManager,
// controllery s Identity pracují jen přes toto rozhraní.
public interface IAccountService
{
    // Vytvoří účet s rolí Ctenar a uživatele rovnou přihlásí
    Task<OperationResult> RegisterAsync(RegisterUserDto dto);
    Task<bool> LoginAsync(string email, string password, bool rememberMe);
    Task LogoutAsync();
    // Změna vlastního hesla přihlášeného uživatele – vyžaduje současné heslo, cizí heslo takto změnit nejde
    Task<OperationResult> ChangePasswordAsync(string userId, string currentPassword, string newPassword);
}
