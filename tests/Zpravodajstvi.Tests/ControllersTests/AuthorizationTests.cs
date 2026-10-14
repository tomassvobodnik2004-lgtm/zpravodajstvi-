using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xunit;
using Zpravodajstvi.Presentation.Areas.Admin.Controllers;
using Zpravodajstvi.Presentation.Controllers;

namespace Zpravodajstvi.Tests.ControllersTests;

// Hlídá oprávnění v administraci: účty a role smí spravovat jen Admin, obsah Admin a Redaktor (Manager)
public class AuthorizationTests
{
    public static TheoryData<Type, string> AdminAreaControllers => new()
    {
        { typeof(UserController), "Admin" },
        { typeof(RoleController), "Admin" },
        { typeof(ArticleController), "Admin,Redaktor" },
        { typeof(CategoryController), "Admin,Redaktor" },
        { typeof(TagController), "Admin,Redaktor" },
        { typeof(CommentController), "Admin,Redaktor" }
    };

    [Theory]
    [MemberData(nameof(AdminAreaControllers))]
    public void AdminAreaController_RequiresExpectedRoles(Type controller, string expectedRoles)
    {
        // Act
        var attribute = Assert.Single(controller.GetCustomAttributes<AuthorizeAttribute>(inherit: true));

        // Assert
        Assert.Equal(expectedRoles, attribute.Roles);
    }

    [Theory]
    [MemberData(nameof(AdminAreaControllers))]
    public void AdminAreaController_NoActionOverridesAuthorization(Type controller, string expectedRoles)
    {
        // Žádná akce nesmí oprávnění controlleru obejít přes [AllowAnonymous] ani ho změnit vlastním [Authorize]
        var actions = controller.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        Assert.NotEmpty(expectedRoles);
        Assert.DoesNotContain(actions, a => a.IsDefined(typeof(AllowAnonymousAttribute)) || a.IsDefined(typeof(AuthorizeAttribute)));
    }

    [Fact]
    public void EveryAdminAreaController_IsCoveredByTheTest()
    {
        // Nový controller v administraci musí být doplněn do seznamu výše (a tím i zkontrolován)
        var adminControllers = typeof(UserController).Assembly.GetTypes()
            .Where(t => t.GetCustomAttribute<AreaAttribute>()?.RouteValue == "Admin")
            .ToHashSet();
        var tested = AdminAreaControllers.Select(row => (Type)row[0]).ToHashSet();

        Assert.Equal(tested, adminControllers);
    }

    [Theory]
    [InlineData(nameof(AccountController.Profile))]
    [InlineData(nameof(AccountController.ChangePassword))]
    [InlineData(nameof(AccountController.Logout))]
    public void AccountActions_RequireSignedInUser(string action)
    {
        // Act
        var methods = typeof(AccountController).GetMethods().Where(m => m.Name == action).ToList();

        // Assert – GET i POST varianta vyžaduje přihlášení
        Assert.NotEmpty(methods);
        Assert.All(methods, m => Assert.True(m.IsDefined(typeof(AuthorizeAttribute))));
    }
}
