using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using Zpravodajstvi.Application.DTOs;
using Zpravodajstvi.Application.Interfaces;
using Zpravodajstvi.Infrastructure.Identity;
using Zpravodajstvi.Presentation.Controllers;

namespace Zpravodajstvi.Tests.ControllersTests;

public class HomeControllerTests
{
    private readonly Mock<IArticleService> _articleServiceMock = new();
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly HomeController _controller;

    public HomeControllerTests()
    {
        var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        _controller = new HomeController(_articleServiceMock.Object, _userManagerMock.Object);
    }

    [Fact]
    public async Task Index_ReturnsViewResultWithArticlesList()
    {
        // Arrange
        var sampleArticles = new List<ArticleListDto>
        {
            new() { Id = 1, Title = "Článek 1", Perex = "Perex 1", CategoryName = "Domácí" },
            new() { Id = 2, Title = "Článek 2", Perex = "Perex 2", CategoryName = "Sport" }
        };

        var sampleCategories = new List<CategoryDto>
        {
            new() { Id = 1, Name = "Domácí", ArticlesCount = 1 },
            new() { Id = 2, Name = "Sport", ArticlesCount = 1 }
        };

        _articleServiceMock
            .Setup(s => s.GetArticlesAsync(null, null, null))
            .ReturnsAsync(sampleArticles);

        _articleServiceMock
            .Setup(s => s.GetCategoriesAsync())
            .ReturnsAsync(sampleCategories);

        // Act
        var result = await _controller.Index();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<IEnumerable<ArticleListDto>>(viewResult.Model);
        Assert.Equal(2, model.Count());
    }

    [Fact]
    public async Task Details_ExistingArticle_ReturnsViewResultWithArticleDetails()
    {
        // Arrange
        var article = new ArticleDetailDto
        {
            Id = 5,
            Title = "Detailní článek",
            Perex = "Perex",
            Content = "Obsah článku",
            CategoryName = "Technologie"
        };

        _articleServiceMock
            .Setup(s => s.GetArticleDetailsAsync(5))
            .ReturnsAsync(article);

        // Act
        var result = await _controller.Details(5);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<ArticleDetailDto>(viewResult.Model);
        Assert.Equal(5, model.Id);
        Assert.Equal("Detailní článek", model.Title);
    }

    [Fact]
    public async Task Details_NonExistingArticle_ReturnsNotFound()
    {
        // Arrange
        _articleServiceMock
            .Setup(s => s.GetArticleDetailsAsync(999))
            .ReturnsAsync((ArticleDetailDto?)null);

        // Act
        var result = await _controller.Details(999);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }
}
