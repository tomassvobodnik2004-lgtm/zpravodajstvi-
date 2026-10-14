using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using Zpravodajstvi.Application.DTOs;
using Zpravodajstvi.Application.Interfaces;
using Zpravodajstvi.Domain.Constants;
using Zpravodajstvi.Presentation.Controllers;

namespace Zpravodajstvi.Tests.ControllersTests;

public class CommentsControllerTests
{
    private const string CurrentUserId = "user-1";
    private const string CurrentUserName = "Karel Čtenář";

    private readonly Mock<ICommentService> _commentServiceMock = new();
    private readonly Mock<IArticleService> _articleServiceMock = new();
    private readonly Mock<IUserService> _userServiceMock = new();
    private readonly CommentsController _controller;

    public CommentsControllerTests()
    {
        _userServiceMock
            .Setup(s => s.GetDisplayNameAsync(CurrentUserId))
            .ReturnsAsync(CurrentUserName);

        _controller = new CommentsController(
            _commentServiceMock.Object,
            _articleServiceMock.Object,
            _userServiceMock.Object,
            NullLogger<CommentsController>.Instance);

        SignIn(AppRoles.Ctenar);
    }

    // Přihlášený uživatel: Id je v claimu NameIdentifier, User.Identity.Name obsahuje e-mail
    private void SignIn(string role)
    {
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, CurrentUserId),
                new Claim(ClaimTypes.Name, "karel@zpravodajstvi.cz"),
                new Claim(ClaimTypes.Role, role)
            ], "TestAuth"))
        };
        _controller.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
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
            .Setup(s => s.AddCommentAsync(model, CurrentUserId))
            .ReturnsAsync(new CommentDto
            {
                Id = 1,
                AuthorName = CurrentUserName,
                Content = model.Content,
                ArticleId = model.ArticleId,
                UserId = CurrentUserId
            });

        // Act
        var result = await _controller.Add(model);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Details", redirectResult.ActionName);
        Assert.Equal("Home", redirectResult.ControllerName);
        Assert.Equal(1, redirectResult.RouteValues?["id"]);
        // Autor se bere z přihlášeného účtu (claim NameIdentifier), ne z formuláře
        _commentServiceMock.Verify(s => s.AddCommentAsync(model, CurrentUserId), Times.Once);
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

        _articleServiceMock
            .Setup(s => s.GetArticleDetailsAsync(1))
            .ReturnsAsync(new ArticleDetailDto { Id = 1, Title = "Testovací článek" });

        // Act
        var result = await _controller.Add(model);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal("~/Views/Home/Details.cshtml", viewResult.ViewName);
        var viewModel = Assert.IsType<ArticleDetailDto>(viewResult.Model);
        Assert.Equal(1, viewModel.Id);
        Assert.Equal(CurrentUserName, viewModel.CurrentUserName);
        _commentServiceMock.Verify(s => s.AddCommentAsync(It.IsAny<CreateCommentDto>(), It.IsAny<string>()), Times.Never);
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
    public async Task Edit_ValidModel_PassesUserIdToServiceAndRedirectsToCommentAnchor()
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

        // Assert – čtenář není moderátor, služba ověří, že jde o jeho komentář
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("comment-5", redirectResult.Fragment);
        _commentServiceMock.Verify(s => s.UpdateCommentAsync(5, "Opravený text komentáře.", CurrentUserId, false), Times.Once);
    }

    [Theory]
    [InlineData(AppRoles.Admin)]
    [InlineData(AppRoles.Redaktor)]
    public async Task Delete_ByModerator_PassesModeratorFlag(string role)
    {
        // Arrange – Admin i Redaktor (správa obsahu) moderují komentáře
        SignIn(role);

        // Act
        await _controller.Delete(7, 1);

        // Assert
        _commentServiceMock.Verify(s => s.DeleteCommentAsync(7, CurrentUserId, true), Times.Once);
    }

    [Fact]
    public async Task Delete_OwnComment_PassesUserIdAndRedirects()
    {
        // Act
        var result = await _controller.Delete(5, 1);

        // Assert – službě se předá Id uživatele, ne e-mail z User.Identity.Name
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Details", redirectResult.ActionName);
        _commentServiceMock.Verify(s => s.DeleteCommentAsync(5, CurrentUserId, false), Times.Once);
    }

    [Fact]
    public async Task Delete_ForeignComment_ReturnsForbid()
    {
        // Arrange
        _commentServiceMock
            .Setup(s => s.DeleteCommentAsync(7, CurrentUserId, false))
            .ThrowsAsync(new UnauthorizedAccessException());

        // Act
        var result = await _controller.Delete(7, 1);

        // Assert
        Assert.IsType<ForbidResult>(result);
    }
}
