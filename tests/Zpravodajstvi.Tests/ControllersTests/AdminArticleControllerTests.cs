using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using Zpravodajstvi.Application.DTOs;
using Zpravodajstvi.Application.Interfaces;
using Zpravodajstvi.Presentation.Areas.Admin.Controllers;
using Zpravodajstvi.Presentation.Areas.Admin.Models;

namespace Zpravodajstvi.Tests.ControllersTests;

// Testy správy článků v administraci – controller pracuje jen se službami (mocky), autora bere z přihlášeného uživatele
public class AdminArticleControllerTests
{
    private const string RedaktorId = "redaktor-1";

    private readonly Mock<IArticleService> _articleServiceMock = new();
    private readonly Mock<ICategoryService> _categoryServiceMock = new();
    private readonly Mock<ITagService> _tagServiceMock = new();
    private readonly ArticleController _controller;

    public AdminArticleControllerTests()
    {
        _categoryServiceMock
            .Setup(s => s.GetAllCategoriesAsync())
            .ReturnsAsync(new List<CategoryDto> { new() { Id = 1, Name = "Sport" } });
        _tagServiceMock
            .Setup(s => s.GetAllTagsAsync())
            .ReturnsAsync(new List<TagDto> { new() { Id = 10, Name = "Hokej" }, new() { Id = 11, Name = "Česko" } });

        _controller = new ArticleController(
            _articleServiceMock.Object,
            _categoryServiceMock.Object,
            _tagServiceMock.Object,
            NullLogger<ArticleController>.Instance)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                        [new Claim(ClaimTypes.NameIdentifier, RedaktorId)], "TestAuth"))
                }
            }
        };
    }

    [Fact]
    public async Task Create_ValidModel_PassesCurrentUserAsAuthor()
    {
        // Arrange
        var model = new ArticleCreateViewModel
        {
            Title = "Nový článek",
            Content = "Text nového článku.",
            CategoryId = 1,
            SelectedTagIds = [10, 11]
        };

        _articleServiceMock
            .Setup(s => s.CreateArticleAsync(It.IsAny<CreateArticleDto>(), RedaktorId))
            .ReturnsAsync(new ArticleDetailDto { Id = 7, Title = model.Title });

        // Act
        var result = await _controller.Create(model);

        // Assert – autorem je přihlášený uživatel z claimu, ne hodnota z formuláře
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        _articleServiceMock.Verify(s => s.CreateArticleAsync(
            It.Is<CreateArticleDto>(d => d.Title == "Nový článek" && d.CategoryId == 1 && d.TagIds.SequenceEqual(new[] { 10, 11 })),
            RedaktorId), Times.Once);
    }

    [Fact]
    public async Task Create_InvalidModel_ReturnsViewWithSelectListsAndDoesNotSave()
    {
        // Arrange
        var model = new ArticleCreateViewModel { Title = "", SelectedTagIds = [11] };
        _controller.ModelState.AddModelError(nameof(ArticleCreateViewModel.Title), "Titulek článku je povinný.");

        // Act
        var result = await _controller.Create(model);

        // Assert – formulář se vrátí s chybou a znovu naplněným výběrem kategorií a štítků
        var viewResult = Assert.IsType<ViewResult>(result);
        var viewModel = Assert.IsType<ArticleCreateViewModel>(viewResult.Model);
        Assert.Single(viewModel.Categories);
        Assert.Equal(2, viewModel.AvailableTags.Count());
        Assert.True(viewModel.AvailableTags.Single(t => t.Value == "11").Selected);
        _articleServiceMock.Verify(s => s.CreateArticleAsync(It.IsAny<CreateArticleDto>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Edit_ValidModel_SendsUpdateDtoToService()
    {
        // Arrange
        var model = new ArticleEditViewModel
        {
            Id = 3,
            Title = "Upravený titulek",
            Content = "Upravený text článku.",
            CategoryId = 1,
            SelectedTagIds = [10]
        };

        // Act
        var result = await _controller.Edit(model);

        // Assert
        Assert.IsType<RedirectToActionResult>(result);
        _articleServiceMock.Verify(s => s.UpdateArticleAsync(
            It.Is<UpdateArticleDto>(d => d.Id == 3 && d.Title == "Upravený titulek" && d.TagIds.Single() == 10)), Times.Once);
    }
}
