using Zpravodajstvi.Application.DTOs;

namespace Zpravodajstvi.Application.Interfaces;

// Správa uživatelských účtů (administrace) a dohledání údajů o uživateli.
// Implementace (Infrastructure) používá UserManager; heslo ani jeho hash se při úpravě nemění.
public interface IUserService
{
    Task<IEnumerable<UserDto>> GetAllUsersAsync();
    Task<UserDto?> GetUserByIdAsync(string id);
    // Jméno pro zobrazení (celé jméno, případně e-mail); null, pokud uživatel neexistuje
    Task<string?> GetDisplayNameAsync(string userId);
    Task<OperationResult> CreateUserAsync(CreateUserDto dto);
    // currentUserId = přihlášený admin – sám sobě nemůže odebrat roli Admin
    Task<OperationResult> UpdateUserAsync(UpdateUserDto dto, string currentUserId);
    // currentUserId = přihlášený admin – nemůže smazat vlastní účet
    Task<OperationResult> DeleteUserAsync(string id, string currentUserId);
}
