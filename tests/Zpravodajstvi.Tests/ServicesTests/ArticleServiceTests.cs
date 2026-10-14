using System.Linq.Expressions;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;
using Zpravodajstvi.Application.DTOs;
using Zpravodajstvi.Application.Interfaces;
using Zpravodajstvi.Application.Services;
using Zpravodajstvi.Domain.Entities;
using Zpravodajstvi.Domain.Interfaces;

namespace Zpravodajstvi.Tests.ServicesTests;

public class ArticleServiceTests
{
    private readonly Mock<IArticleRepository> _articleRepoMock = new();
    private readonly Mock<ITagRepository> _tagRepoMock = new();
    private readonly Mock<IFileStorageService> _fileStorageMock = new();
    private readonly ArticleService _service;

    public ArticleServiceTests()
    {
        _service = new ArticleService(_articleRepoMock.Object, _tagRepoMock.Object, _fileStorageMock.Object);
    }

    private static IFormFile CreateImage(string fileName)
    {
        var file = new Mock<IFormFile>();
        file.Setup(f => f.FileName).Returns(fileName);
        file.Setup(f => f.Length).Returns(1024);
        return file.Object;
    }

    [Fact]
    public async Task CreateArticleAsync_SetsAuthorTagsAndUploadedImage()
    {
        // Arrange
        var hokej = new Tag { Id = 10, Name = "Hokej" };
        _tagRepoMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Tag, bool>>>()))
            .ReturnsAsync(new List<Tag> { hokej });
        _fileStorageMock
            .Setup(s => s.SaveImageAsync(It.IsAny<IFormFile>()))
            .ReturnsAsync("/uploads/abc_foto.jpg");
        _articleRepoMock
            .Setup(r => r.AddAsync(It.IsAny<Article>()))
            .ReturnsAsync((Article a) => { a.Id = 42; return a; });

        var dto = new CreateArticleDto
        {
            Title = "Výhra v prodloužení",
            Content = "Text článku o hokeji.",
            CategoryId = 3,
            TagIds = [10],
            Image = CreateImage("foto.jpg")
        };

        // Act
        var result = await _service.CreateArticleAsync(dto, "redaktor-1");

        // Assert – autor je Id přihlášeného uživatele, obrázek uložila služba pro soubory
        Assert.Equal(42, result.Id);
        Assert.Equal("redaktor-1", result.AuthorId);
        _articleRepoMock.Verify(r => r.AddAsync(It.Is<Article>(a =>
            a.AuthorId == "redaktor-1" &&
            a.Tags.Single() == hokej &&
            a.Images.Single().Url == "/uploads/abc_foto.jpg" &&
            a.Perex == "Text článku o hokeji.")), Times.Once);
    }

    [Fact]
    public async Task UpdateArticleAsync_NewImage_ReplacesOldImageAndDeletesItsFile()
    {
        // Arrange
        var article = new Article
        {
            Id = 5,
            Title = "Starý titulek",
            Content = "Starý text",
            Images = new List<Image> { new() { Id = 1, Url = "/uploads/old.jpg", ArticleId = 5 } },
            Tags = new List<Tag> { new() { Id = 1, Name = "Starý" } }
        };
        _articleRepoMock.Setup(r => r.GetByIdWithDetailsAsync(5)).ReturnsAsync(article);
        _tagRepoMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Tag, bool>>>()))
            .ReturnsAsync(new List<Tag>());
        _fileStorageMock
            .Setup(s => s.SaveImageAsync(It.IsAny<IFormFile>()))
            .ReturnsAsync("/uploads/new.jpg");

        // Act
        await _service.UpdateArticleAsync(new UpdateArticleDto
        {
            Id = 5,
            Title = "Nový titulek",
            Content = "Nový text",
            CategoryId = 2,
            TagIds = [],
            Image = CreateImage("new.jpg")
        });

        // Assert
        Assert.Equal("Nový titulek", article.Title);
        Assert.Equal(2, article.CategoryId);
        Assert.Empty(article.Tags);
        Assert.Equal("/uploads/new.jpg", Assert.Single(article.Images).Url);
        Assert.NotNull(article.UpdatedAt);
        _articleRepoMock.Verify(r => r.UpdateAsync(article), Times.Once);
        _fileStorageMock.Verify(s => s.DeleteImage("/uploads/old.jpg"), Times.Once);
    }

    [Fact]
    public async Task GetArticleDetailsAsync_MapsAuthorOfArticleAndComments()
    {
        // Arrange
        var redaktor = new ApplicationUser { Id = "r-1", FullName = "Jan Redaktor", Email = "redaktor@zpravodajstvi.cz" };
        var ctenar = new ApplicationUser { Id = "c-1", FullName = "Petr Čtenář" };
        _articleRepoMock.Setup(r => r.GetByIdWithDetailsAsync(1)).ReturnsAsync(new Article
        {
            Id = 1,
            Title = "Článek",
            Category = new Category { Id = 1, Name = "Sport" },
            AuthorId = redaktor.Id,
            Author = redaktor,
            Comments = new List<Comment>
            {
                new() { Id = 7, ArticleId = 1, UserId = ctenar.Id, User = ctenar, AuthorName = "Petr Čtenář", Content = "Super" }
            }
        });

        // Act
        var result = await _service.GetArticleDetailsAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Jan Redaktor", result.AuthorName);
        Assert.Equal("Petr Čtenář", Assert.Single(result.Comments).AuthorName);
    }

    [Fact]
    public async Task GetArticlesAsync_ArticleWithoutAuthor_ShowsUnknownAuthor()
    {
        // Arrange – autor byl smazán (AuthorId = null díky SetNull)
        _articleRepoMock
            .Setup(r => r.GetFilteredAsync(null, null, null, null))
            .ReturnsAsync(new List<Article> { new() { Id = 1, Title = "Článek", AuthorId = null } });

        // Act
        var result = (await _service.GetArticlesAsync()).Single();

        // Assert
        Assert.Equal("Neznámý autor", result.AuthorName);
    }

    [Fact]
    public async Task DeleteArticleAsync_DeletesArticleAndItsUploadedFiles()
    {
        // Arrange
        var article = new Article
        {
            Id = 9,
            Images = new List<Image> { new() { Url = "/uploads/a.jpg" }, new() { Url = "https://example.com/b.jpg" } }
        };
        _articleRepoMock.Setup(r => r.GetByIdWithDetailsAsync(9)).ReturnsAsync(article);

        // Act
        await _service.DeleteArticleAsync(9);

        // Assert – o tom, který soubor je lokální, rozhoduje IFileStorageService
        _articleRepoMock.Verify(r => r.DeleteAsync(article), Times.Once);
        _fileStorageMock.Verify(s => s.DeleteImage("/uploads/a.jpg"), Times.Once);
        _fileStorageMock.Verify(s => s.DeleteImage("https://example.com/b.jpg"), Times.Once);
    }
}
