using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using Zpravodajstvi.Application.DTOs;
using Zpravodajstvi.Application.Interfaces;
using Zpravodajstvi.Presentation.Controllers;
using Zpravodajstvi.Presentation.Models;

namespace Zpravodajstvi.Tests.ControllersTests;

// Změna vlastního hesla: controller jen předá data službě, Id uživatele bere z přihlášení
public class AccountControllerTests
{
    private const string CurrentUserId = "user-1";

    private readonly Mock<IAccountService> _accountServiceMock = new();
    private readonly AccountController _controller;

    public AccountControllerTests()
    {
        _controller = new AccountController(
            _accountServiceMock.Object,
            Mock.Of<IUserService>(),
            Mock.Of<IArticleService>(),
            Mock.Of<ICommentService>(),
            NullLogger<AccountController>.Instance);

        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, CurrentUserId)], "TestAuth"))
        };
        _controller.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
    }

    private static ChangePasswordViewModel ValidModel() => new()
    {
        CurrentPassword = "Heslo123!",
        NewPassword = "NoveHeslo456",
        ConfirmNewPassword = "NoveHeslo456"
    };

    [Fact]
    public async Task ChangePassword_Valid_CallsServiceForCurrentUserAndRedirectsToProfile()
    {
        // Arrange
        _accountServiceMock
            .Setup(s => s.ChangePasswordAsync(CurrentUserId, "Heslo123!", "NoveHeslo456"))
            .ReturnsAsync(OperationResult.Success());

        // Act
        var result = await _controller.ChangePassword(ValidModel());

        // Assert – heslo se mění jen přihlášenému uživateli (Id z claimu)
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(AccountController.Profile), redirect.ActionName);
        _accountServiceMock.Verify(s => s.ChangePasswordAsync(CurrentUserId, "Heslo123!", "NoveHeslo456"), Times.Once);
    }

    [Fact]
    public async Task ChangePassword_InvalidModel_ReturnsViewAndDoesNotCallService()
    {
        // Arrange – potvrzení se neshoduje ([Compare])
        _controller.ModelState.AddModelError(nameof(ChangePasswordViewModel.ConfirmNewPassword), "Nové heslo a jeho potvrzení se neshodují.");

        // Act
        var result = await _controller.ChangePassword(ValidModel());

        // Assert
        Assert.IsType<ViewResult>(result);
        _accountServiceMock.Verify(s => s.ChangePasswordAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ChangePassword_WrongCurrentPassword_ShowsServiceError()
    {
        // Arrange
        _accountServiceMock
            .Setup(s => s.ChangePasswordAsync(CurrentUserId, It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(OperationResult.Failure("Současné heslo není správné."));

        // Act
        var result = await _controller.ChangePassword(ValidModel());

        // Assert
        Assert.IsType<ViewResult>(result);
        var error = Assert.Single(_controller.ModelState[string.Empty]!.Errors);
        Assert.Equal("Současné heslo není správné.", error.ErrorMessage);
    }
}
