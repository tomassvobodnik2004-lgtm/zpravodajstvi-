namespace Zpravodajstvi.Application.DTOs;

// Uživatel pro výpis a formuláře. Záměrně neobsahuje PasswordHash ani jiné citlivé údaje.
public class UserDto
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

// Registrace nového čtenáře (veřejný formulář)
public class RegisterUserDto
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

// Vytvoření účtu administrátorem – počáteční heslo nastaví admin
public class CreateUserDto
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

// Úprava účtu administrátorem. Heslo (ani jeho hash) tu není – admin ho měnit nemůže.
public class UpdateUserDto
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

public class RoleDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int UsersCount { get; set; }
    // Systémová role (Admin, Redaktor, Ctenar) – nejde přejmenovat ani smazat
    public bool IsSystemRole { get; set; }
}
