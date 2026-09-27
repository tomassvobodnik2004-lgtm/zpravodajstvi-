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

    public CommentsControllerTests()
    {
        var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        _controller = new CommentsController(
            _commentServiceMock.Object,
            _articleServiceMock.Object,
            _userManagerMock.Object);

        // Nastavení TempData pro controller
        var httpContext = new DefaultHttpContext();
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
            AuthorName = "Karel Čtenář",
            Content = "Pěkně sepsáno!"
        };

        _commentServiceMock
            .Setup(s => s.AddCommentAsync(model))
            .ReturnsAsync(new CommentDto
            {
                Id = 1,
                AuthorName = model.AuthorName,
                Content = model.Content,
                ArticleId = model.ArticleId
            });

        // Act
        var result = await _controller.Add(model);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Details", redirectResult.ActionName);
        Assert.Equal("Home", redirectResult.ControllerName);
        Assert.Equal(1, redirectResult.RouteValues?["id"]);
        _commentServiceMock.Verify(s => s.AddCommentAsync(model), Times.Once);
    }

    [Fact]
    public async Task Add_InvalidModelState_ReturnsArticleDetailsViewWithModel()
    {
        // Arrange
        var model = new CreateCommentDto
        {
            ArticleId = 1,
            AuthorName = "Karel Čtenář",
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
        _commentServiceMock.Verify(s => s.AddCommentAsync(It.IsAny<CreateCommentDto>()), Times.Never);
    }
}
