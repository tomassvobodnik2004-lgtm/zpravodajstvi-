using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using Zpravodajstvi.Application.DTOs;
using Zpravodajstvi.Application.Interfaces;
using Zpravodajstvi.Presentation.Areas.Admin.Controllers;
using Zpravodajstvi.Presentation.Areas.Admin.Models;

namespace Zpravodajstvi.Tests.ControllersTests;

public class CategoryControllerTests
{
    private readonly Mock<ICategoryService> _categoryServiceMock = new();
    private readonly CategoryController _controller;

    public CategoryControllerTests()
    {
        _controller = new CategoryController(_categoryServiceMock.Object, NullLogger<CategoryController>.Instance);
    }

    [Fact]
    public async Task DeleteConfirmed_CategoryWithArticles_ReturnsDeleteViewWithMessage()
    {
        // Arrange – služba kategorii s články odmítne smazat
        const string message = "Rubriku nelze smazat, obsahuje 3 články. Nejprve je přesuňte nebo smažte.";
        var category = new CategoryDto { Id = 3, Name = "Sport", ArticlesCount = 3 };
        _categoryServiceMock
            .Setup(s => s.DeleteCategoryAsync(3))
            .ThrowsAsync(new InvalidOperationException(message));
        _categoryServiceMock
            .Setup(s => s.GetCategoryByIdAsync(3))
            .ReturnsAsync(category);

        // Act
        var result = await _controller.DeleteConfirmed(3);

        // Assert – vrátí se stránka mazání se srozumitelnou hláškou místo chyby 500
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Same(category, viewResult.Model);
        var error = Assert.Single(_controller.ModelState[string.Empty]!.Errors);
        Assert.Equal(message, error.ErrorMessage);
    }

    [Fact]
    public async Task DeleteConfirmed_EmptyCategory_DeletesAndRedirectsToIndex()
    {
        // Act
        var result = await _controller.DeleteConfirmed(4);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
        _categoryServiceMock.Verify(s => s.DeleteCategoryAsync(4), Times.Once);
    }

    [Fact]
    public async Task Edit_InvalidModel_ReturnsViewAndDoesNotUpdate()
    {
        // Arrange
        var model = new CategoryViewModel { Id = 1, Name = "" };
        _controller.ModelState.AddModelError(nameof(CategoryViewModel.Name), "Název kategorie je povinný.");

        // Act
        var result = await _controller.Edit(model);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Same(model, viewResult.Model);
        _categoryServiceMock.Verify(s => s.UpdateCategoryAsync(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Edit_ValidModel_UpdatesAndRedirectsToIndex()
    {
        // Act
        var result = await _controller.Edit(new CategoryViewModel { Id = 1, Name = "Věda" });

        // Assert
        Assert.IsType<RedirectToActionResult>(result);
        _categoryServiceMock.Verify(s => s.UpdateCategoryAsync(1, "Věda"), Times.Once);
    }
}
