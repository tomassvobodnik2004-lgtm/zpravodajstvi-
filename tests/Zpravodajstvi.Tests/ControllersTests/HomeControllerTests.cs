using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using Zpravodajstvi.Application.DTOs;
using Zpravodajstvi.Application.Interfaces;
using Zpravodajstvi.Presentation.Controllers;

namespace Zpravodajstvi.Tests.ControllersTests;

public class HomeControllerTests
{
    private readonly Mock<IArticleService> _articleServiceMock = new();
    private readonly Mock<ICategoryService> _categoryServiceMock = new();
    private readonly Mock<IUserService> _userServiceMock = new();
    private readonly HomeController _controller;

    public HomeControllerTests()
    {
        _controller = new HomeController(_articleServiceMock.Object, _categoryServiceMock.Object, _userServiceMock.Object);
    }

    [Fact]
    public async Task Index_ReturnsViewResultWithArticlesList()
    {
        // Arrange
        var sampleArticles = new List<ArticleListDto>
        {
            new() { Id = 1, Title = "Článek 1", Perex = "Perex 1", CategoryName = "Domácí", AuthorName = "Jan Redaktor" },
            new() { Id = 2, Title = "Článek 2", Perex = "Perex 2", CategoryName = "Sport", AuthorName = "Jan Redaktor" }
        };

        var sampleCategories = new List<CategoryDto>
        {
            new() { Id = 1, Name = "Domácí", ArticlesCount = 1 },
            new() { Id = 2, Name = "Sport", ArticlesCount = 1 }
        };

        _articleServiceMock
            .Setup(s => s.GetArticlesAsync(null, null, null))
            .ReturnsAsync(sampleArticles);

        _categoryServiceMock
            .Setup(s => s.GetAllCategoriesAsync())
            .ReturnsAsync(sampleCategories);

        // Act
        var result = await _controller.Index();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<IEnumerable<ArticleListDto>>(viewResult.Model);
        Assert.Equal(2, model.Count());
        Assert.Same(sampleCategories, viewResult.ViewData["Categories"]);
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
            CategoryName = "Technologie",
            AuthorName = "Jan Redaktor"
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
        Assert.Equal("Jan Redaktor", model.AuthorName);
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
