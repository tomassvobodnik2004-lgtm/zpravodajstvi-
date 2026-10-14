using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;
using Zpravodajstvi.Application.DTOs;
using Zpravodajstvi.Domain.Constants;
using Zpravodajstvi.Domain.Entities;
using Zpravodajstvi.Infrastructure.Data;
using Zpravodajstvi.Infrastructure.Identity;

namespace Zpravodajstvi.Tests.IdentityTests;

// Služby pro účty a role se testují se skutečným ASP.NET Core Identity nad SQLite databází v paměti
// (vytvořenou migracemi), aby se ověřilo chování UserManageru a cizích klíčů
public class IdentityServicesTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ServiceProvider _provider;
    private readonly IServiceScope _scope;
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly UserService _userService;
    private readonly RoleService _roleService;
    private readonly Mock<SignInManager<ApplicationUser>> _signInManagerMock;
    private readonly AccountService _accountService;

    private readonly ApplicationUser _admin;
    private readonly ApplicationUser _ctenar;

    public IdentityServicesTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(_connection));
        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequireDigit = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequiredLength = 6;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>();

        _provider = services.BuildServiceProvider();
        _scope = _provider.CreateScope();
        _context = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        _context.Database.Migrate();

        _userManager = _scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = _scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in AppRoles.SystemRoles)
        {
            roleManager.CreateAsync(new IdentityRole(role)).GetAwaiter().GetResult();
        }

        _userService = new UserService(_userManager, roleManager);
        _roleService = new RoleService(roleManager, _userManager);

        // SignInManager potřebuje HTTP kontext (cookie), proto je nahrazený mockem; hesla ověřuje skutečný UserManager
        _signInManagerMock = new Mock<SignInManager<ApplicationUser>>(
            _userManager,
            Mock.Of<IHttpContextAccessor>(),
            Mock.Of<IUserClaimsPrincipalFactory<ApplicationUser>>(),
            null!, null!, null!, null!);
        _accountService = new AccountService(_userManager, _signInManagerMock.Object);

        _admin = CreateUser("admin@test.cz", "Admin Testovací", AppRoles.Admin);
        _ctenar = CreateUser("ctenar@test.cz", "Petr Čtenář", AppRoles.Ctenar);
    }

    private ApplicationUser CreateUser(string email, string fullName, string role)
    {
        var user = new ApplicationUser { UserName = email, Email = email, FullName = fullName };
        _userManager.CreateAsync(user, "Heslo123!").GetAwaiter().GetResult();
        _userManager.AddToRoleAsync(user, role).GetAwaiter().GetResult();
        return user;
    }

    [Fact]
    public async Task UpdateUserAsync_ChangesNameEmailAndRole_ButNotPasswordHash()
    {
        // Arrange
        var originalHash = _ctenar.PasswordHash;

        // Act
        var result = await _userService.UpdateUserAsync(new UpdateUserDto
        {
            Id = _ctenar.Id,
            FullName = "Petr Redaktor",
            Email = "petr@test.cz",
            Role = AppRoles.Redaktor
        }, _admin.Id);

        // Assert – heslo se úpravou účtu nezmění, přihlásit se jde pořád původním heslem
        Assert.True(result.Succeeded);
        var updated = await _userManager.FindByIdAsync(_ctenar.Id);
        Assert.NotNull(updated);
        Assert.Equal("Petr Redaktor", updated.FullName);
        Assert.Equal("petr@test.cz", updated.Email);
        Assert.Equal(originalHash, updated.PasswordHash);
        Assert.True(await _userManager.CheckPasswordAsync(updated, "Heslo123!"));
        Assert.Equal([AppRoles.Redaktor], await _userManager.GetRolesAsync(updated));
    }

    [Fact]
    public async Task UpdateUserAsync_AdminCannotRemoveOwnAdminRole()
    {
        // Act
        var result = await _userService.UpdateUserAsync(new UpdateUserDto
        {
            Id = _admin.Id,
            FullName = _admin.FullName,
            Email = _admin.Email!,
            Role = AppRoles.Ctenar
        }, _admin.Id);

        // Assert
        Assert.False(result.Succeeded);
        Assert.True(await _userManager.IsInRoleAsync(_admin, AppRoles.Admin));
    }

    [Fact]
    public async Task DeleteUserAsync_OwnAccount_IsRejected()
    {
        // Act
        var result = await _userService.DeleteUserAsync(_admin.Id, _admin.Id);

        // Assert
        Assert.False(result.Succeeded);
        Assert.NotNull(await _userManager.FindByIdAsync(_admin.Id));
    }

    [Fact]
    public async Task DeleteUserAsync_KeepsArticlesAndCommentsWithoutAuthor()
    {
        // Arrange – autor článku a komentáře
        var article = new Article
        {
            Title = "Článek",
            Perex = "Perex",
            Content = "Obsah",
            Category = new Category { Name = "Sport" },
            AuthorId = _ctenar.Id,
            Comments = new List<Comment> { new() { AuthorName = _ctenar.FullName, UserId = _ctenar.Id, Content = "Komentář" } }
        };
        _context.Articles.Add(article);
        await _context.SaveChangesAsync();

        // Act
        var result = await _userService.DeleteUserAsync(_ctenar.Id, _admin.Id);

        // Assert – cizí klíče mají DeleteBehavior.SetNull: článek i komentář zůstanou bez autora
        Assert.True(result.Succeeded);
        _context.ChangeTracker.Clear();
        var savedArticle = await _context.Articles.Include(a => a.Comments).SingleAsync();
        Assert.Null(savedArticle.AuthorId);
        Assert.Null(Assert.Single(savedArticle.Comments).UserId);
    }

    [Fact]
    public async Task ChangePasswordAsync_CorrectCurrentPassword_ChangesHashAndRefreshesSignIn()
    {
        // Arrange
        var originalHash = _ctenar.PasswordHash;

        // Act
        var result = await _accountService.ChangePasswordAsync(_ctenar.Id, "Heslo123!", "NoveHeslo456");

        // Assert – uživatel se přihlásí jen novým heslem a jeho přihlášení (cookie) se obnoví
        Assert.True(result.Succeeded);
        var updated = await _userManager.FindByIdAsync(_ctenar.Id);
        Assert.NotNull(updated);
        Assert.NotEqual(originalHash, updated.PasswordHash);
        Assert.True(await _userManager.CheckPasswordAsync(updated, "NoveHeslo456"));
        Assert.False(await _userManager.CheckPasswordAsync(updated, "Heslo123!"));
        _signInManagerMock.Verify(s => s.RefreshSignInAsync(updated), Times.Once);
    }

    [Fact]
    public async Task ChangePasswordAsync_WrongCurrentPassword_FailsInCzechAndKeepsPassword()
    {
        // Arrange
        var originalHash = _ctenar.PasswordHash;

        // Act
        var result = await _accountService.ChangePasswordAsync(_ctenar.Id, "SpatneHeslo", "NoveHeslo456");

        // Assert
        Assert.False(result.Succeeded);
        Assert.Equal("Současné heslo není správné.", Assert.Single(result.Errors));
        Assert.Equal(originalHash, (await _userManager.FindByIdAsync(_ctenar.Id))!.PasswordHash);
        _signInManagerMock.Verify(s => s.RefreshSignInAsync(It.IsAny<ApplicationUser>()), Times.Never);
    }

    [Fact]
    public async Task GetDisplayNameAsync_ReturnsFullName()
    {
        Assert.Equal("Petr Čtenář", await _userService.GetDisplayNameAsync(_ctenar.Id));
        Assert.Null(await _userService.GetDisplayNameAsync("neexistuje"));
    }

    [Fact]
    public async Task SystemRole_CannotBeRenamedOrDeleted()
    {
        // Arrange
        var adminRole = (await _roleService.GetAllRolesAsync()).Single(r => r.Name == AppRoles.Admin);

        // Act
        var rename = await _roleService.RenameRoleAsync(adminRole.Id, "Spravce");
        var delete = await _roleService.DeleteRoleAsync(adminRole.Id);

        // Assert
        Assert.True(adminRole.IsSystemRole);
        Assert.False(rename.Succeeded);
        Assert.False(delete.Succeeded);
        Assert.Contains(await _roleService.GetAllRolesAsync(), r => r.Name == AppRoles.Admin);
    }

    [Fact]
    public async Task CustomRole_CanBeCreatedRenamedAndDeleted()
    {
        // Act & Assert
        Assert.True((await _roleService.CreateRoleAsync("Moderator")).Succeeded);
        Assert.False((await _roleService.CreateRoleAsync("Moderator")).Succeeded); // duplicitní název

        var role = (await _roleService.GetAllRolesAsync()).Single(r => r.Name == "Moderator");
        Assert.False(role.IsSystemRole);

        Assert.True((await _roleService.RenameRoleAsync(role.Id, "Korektor")).Succeeded);
        Assert.True((await _roleService.DeleteRoleAsync(role.Id)).Succeeded);
        Assert.DoesNotContain(await _roleService.GetAllRolesAsync(), r => r.Name is "Moderator" or "Korektor");
    }

    public void Dispose()
    {
        _scope.Dispose();
        _provider.Dispose();
        _connection.Dispose();
    }
}
