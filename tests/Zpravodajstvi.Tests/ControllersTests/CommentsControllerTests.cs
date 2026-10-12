using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using Xunit;
using Zpravodajstvi.Application.DTOs;
using Zpravodajstvi.Application.Interfaces;
using Zpravodajstvi.Infrastructure.Identity;
using Zpravodajstvi.Presentation.Controllers;

namespace Zpravodajstvi.Tests.ControllersTests;

public class CommentsControllerTests
{
    private readonly Mock<ICommentService> _commentServiceMock = new();
    private readonly Mock<IArticleService> _articleServiceMock = new();
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly CommentsController _controller;

    // Přihlášený čtenář: Id je v claimu NameIdentifier, User.Identity.Name obsahuje e-mail
    private readonly ApplicationUser _currentUser = new()
    {
        Id = "user-1",
        UserName = "karel@zpravodajstvi.cz",
        Email = "karel@zpravodajstvi.cz",
        FullName = "Karel Čtenář"
    };

    public CommentsControllerTests()
    {
        var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        _userManagerMock
            .Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(_currentUser);

        _controller = new CommentsController(
            _commentServiceMock.Object,
            _articleServiceMock.Object,
            _userManagerMock.Object);

        // Nastavení TempData a přihlášeného uživatele pro controller
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, _currentUser.Id),
                new Claim(ClaimTypes.Name, _currentUser.Email!)
            ], "TestAuth"))
        };
        var tempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
        _controller.TempData = tempData;
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    [Fact]
    public async Task Add_ValidComment_AddsCommentAndRedirectsToArticleDetails()
    {
        // Arrange
        var model = new CreateCommentDto
        {
            ArticleId = 1,
            Content = "Pěkně sepsáno!"
        };

        _commentServiceMock
            .Setup(s => s.AddCommentAsync(model, "user-1", "Karel Čtenář"))
            .ReturnsAsync(new CommentDto
            {
                Id = 1,
                AuthorName = "Karel Čtenář",
                Content = model.Content,
                ArticleId = model.ArticleId,
                UserId = "user-1"
            });

        // Act
        var result = await _controller.Add(model);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Details", redirectResult.ActionName);
        Assert.Equal("Home", redirectResult.ControllerName);
        Assert.Equal(1, redirectResult.RouteValues?["id"]);
        // Autor (Id i jméno) se bere z přihlášeného účtu, ne z formuláře
        _commentServiceMock.Verify(s => s.AddCommentAsync(model, "user-1", "Karel Čtenář"), Times.Once);
    }

    [Fact]
    public async Task Add_InvalidModelState_ReturnsArticleDetailsViewWithModel()
    {
        // Arrange
        var model = new CreateCommentDto
        {
            ArticleId = 1,
            Content = "" // Nevalidní
        };

        _controller.ModelState.AddModelError("Content", "Obsah komentáře je povinný.");

        var articleDetail = new ArticleDetailDto
        {
            Id = 1,
            Title = "Testovací článek"
        };

        _articleServiceMock
            .Setup(s => s.GetArticleDetailsAsync(1))
            .ReturnsAsync(articleDetail);

        // Act
        var result = await _controller.Add(model);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal("~/Views/Home/Details.cshtml", viewResult.ViewName);
        var viewModel = Assert.IsType<ArticleDetailDto>(viewResult.Model);
        Assert.Equal(1, viewModel.Id);
        Assert.Equal("Karel Čtenář", viewModel.CurrentUserName);
        _commentServiceMock.Verify(s => s.AddCommentAsync(It.IsAny<CreateCommentDto>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Edit_InvalidModelState_ReturnsArticleDetailsViewAndDoesNotUpdate()
    {
        // Arrange – text s vulgárním slovem neprošel atributem [NoProfanity]
        var model = new UpdateComment
        {
            Id = 5,
            ArticleId = 1,
            Content = "Autor je kretén."
        };

        _controller.ModelState.AddModelError("EditComment.Content", "Komentář obsahuje nepovolená nebo vulgární slova.");

        _articleServiceMock
            .Setup(s => s.GetArticleDetailsAsync(1))
            .ReturnsAsync(new ArticleDetailDto { Id = 1, Title = "Testovací článek" });

        // Act
        var result = await _controller.Edit(model);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal("~/Views/Home/Details.cshtml", viewResult.ViewName);
        var viewModel = Assert.IsType<ArticleDetailDto>(viewResult.Model);
        Assert.Same(model, viewModel.EditComment);
        _commentServiceMock.Verify(s => s.UpdateCommentAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task Edit_ValidModel_PassesUserIdToService()
    {
        // Arrange
        var model = new UpdateComment
        {
            Id = 5,
            ArticleId = 1,
            Content = "Opravený text komentáře."
        };

        // Act
        var result = await _controller.Edit(model);

        // Assert
        Assert.IsType<RedirectToActionResult>(result);
        _commentServiceMock.Verify(s => s.UpdateCommentAsync(5, "Opravený text komentáře.", "user-1", false), Times.Once);
    }

    [Fact]
    public async Task Delete_OwnComment_PassesUserIdAndRedirects()
    {
        // Act
        var result = await _controller.Delete(5, 1);

        // Assert – službě se předá Id uživatele, ne e-mail z User.Identity.Name
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Details", redirectResult.ActionName);
        _commentServiceMock.Verify(s => s.DeleteCommentAsync(5, "user-1", false), Times.Once);
    }

    [Fact]
    public async Task Delete_ForeignComment_ReturnsForbid()
    {
        // Arrange
        _commentServiceMock
            .Setup(s => s.DeleteCommentAsync(7, "user-1", false))
            .ThrowsAsync(new UnauthorizedAccessException());

        // Act
        var result = await _controller.Delete(7, 1);

        // Assert
        Assert.IsType<ForbidResult>(result);
    }
}
