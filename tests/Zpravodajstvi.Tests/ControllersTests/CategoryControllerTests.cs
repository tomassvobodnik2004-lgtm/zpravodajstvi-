using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using Zpravodajstvi.Application.DTOs;
using Zpravodajstvi.Application.Interfaces;
using Zpravodajstvi.Presentation.Areas.Admin.Controllers;

namespace Zpravodajstvi.Tests.ControllersTests;

public class CategoryControllerTests
{
    private readonly Mock<ICategoryService> _categoryServiceMock = new();
    private readonly CategoryController _controller;

    public CategoryControllerTests()
    {
        _controller = new CategoryController(_categoryServiceMock.Object);
    }

    [Theory]
    [InlineData(1, "Rubriku nelze smazat, obsahuje 1 článek. Nejprve ho přesuňte nebo smažte.")]
    [InlineData(3, "Rubriku nelze smazat, obsahuje 3 články. Nejprve je přesuňte nebo smažte.")]
    [InlineData(5, "Rubriku nelze smazat, obsahuje 5 článků. Nejprve je přesuňte nebo smažte.")]
    public async Task DeleteConfirmed_CategoryWithArticles_ReturnsDeleteViewWithMessage(int articlesCount, string expectedMessage)
    {
        // Arrange
        var category = new CategoryDto { Id = 3, Name = "Sport", ArticlesCount = articlesCount };
        _categoryServiceMock
            .Setup(s => s.GetCategoryByIdAsync(3))
            .ReturnsAsync(category);

        // Act
        var result = await _controller.DeleteConfirmed(3);

        // Assert – rubrika se nesmaže, vrátí se stránka mazání se srozumitelnou hláškou
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Same(category, viewResult.Model);
        var error = Assert.Single(_controller.ModelState[string.Empty]!.Errors);
        Assert.Equal(expectedMessage, error.ErrorMessage);
        _categoryServiceMock.Verify(s => s.DeleteCategoryAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task DeleteConfirmed_EmptyCategory_DeletesAndRedirectsToIndex()
    {
        // Arrange
        var category = new CategoryDto { Id = 4, Name = "Zahraničí", ArticlesCount = 0 };
        _categoryServiceMock
            .Setup(s => s.GetCategoryByIdAsync(4))
            .ReturnsAsync(category);

        // Act
        var result = await _controller.DeleteConfirmed(4);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
        _categoryServiceMock.Verify(s => s.DeleteCategoryAsync(4), Times.Once);
    }
}
