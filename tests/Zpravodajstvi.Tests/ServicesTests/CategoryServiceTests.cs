using Moq;
using Xunit;
using Zpravodajstvi.Application.Services;
using Zpravodajstvi.Domain.Entities;
using Zpravodajstvi.Domain.Interfaces;

namespace Zpravodajstvi.Tests.ServicesTests;

public class CategoryServiceTests
{
    private readonly Mock<ICategoryRepository> _repositoryMock = new();
    private readonly CategoryService _service;

    public CategoryServiceTests()
    {
        _service = new CategoryService(_repositoryMock.Object);
    }

    [Theory]
    [InlineData(1, "Rubriku nelze smazat, obsahuje 1 článek. Nejprve ho přesuňte nebo smažte.")]
    [InlineData(3, "Rubriku nelze smazat, obsahuje 3 články. Nejprve je přesuňte nebo smažte.")]
    [InlineData(5, "Rubriku nelze smazat, obsahuje 5 článků. Nejprve je přesuňte nebo smažte.")]
    public async Task DeleteCategoryAsync_CategoryWithArticles_ThrowsWithCzechMessage(int articlesCount, string expectedMessage)
    {
        // Arrange
        var category = new Category
        {
            Id = 3,
            Name = "Sport",
            Articles = Enumerable.Range(1, articlesCount).Select(i => new Article { Id = i }).ToList()
        };
        _repositoryMock.Setup(r => r.GetByIdWithArticlesAsync(3)).ReturnsAsync(category);

        // Act & Assert – vazba Category -> Article má Restrict, kategorie se nesmaže
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.DeleteCategoryAsync(3));
        Assert.Equal(expectedMessage, exception.Message);
        _repositoryMock.Verify(r => r.DeleteAsync(It.IsAny<Category>()), Times.Never);
    }

    [Fact]
    public async Task DeleteCategoryAsync_EmptyCategory_Deletes()
    {
        // Arrange
        var category = new Category { Id = 4, Name = "Zahraničí" };
        _repositoryMock.Setup(r => r.GetByIdWithArticlesAsync(4)).ReturnsAsync(category);

        // Act
        await _service.DeleteCategoryAsync(4);

        // Assert
        _repositoryMock.Verify(r => r.DeleteAsync(category), Times.Once);
    }

    [Fact]
    public async Task UpdateCategoryAsync_TrimsAndSavesName()
    {
        // Arrange
        var category = new Category { Id = 1, Name = "Technologie" };
        _repositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(category);

        // Act
        await _service.UpdateCategoryAsync(1, "  Věda a technika  ");

        // Assert
        Assert.Equal("Věda a technika", category.Name);
        _repositoryMock.Verify(r => r.UpdateAsync(category), Times.Once);
    }
}
