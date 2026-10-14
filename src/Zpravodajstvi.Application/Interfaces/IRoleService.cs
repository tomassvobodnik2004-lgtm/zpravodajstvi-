using Zpravodajstvi.Application.DTOs;

namespace Zpravodajstvi.Application.Interfaces;

// Správa rolí (administrace). Implementace (Infrastructure) používá RoleManager.
public interface IRoleService
{
    Task<IEnumerable<RoleDto>> GetAllRolesAsync();
    Task<RoleDto?> GetRoleByIdAsync(string id);
    Task<OperationResult> CreateRoleAsync(string name);
    // Systémové role (Admin, Redaktor, Ctenar) přejmenovat ani smazat nelze
    Task<OperationResult> RenameRoleAsync(string id, string newName);
    Task<OperationResult> DeleteRoleAsync(string id);
}
