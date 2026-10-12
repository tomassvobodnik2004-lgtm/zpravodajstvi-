using Moq;
using Xunit;
using Zpravodajstvi.Application.DTOs;
using Zpravodajstvi.Application.Interfaces;
using Zpravodajstvi.Application.Services;
using Zpravodajstvi.Domain.Entities;
using Zpravodajstvi.Domain.Interfaces;

namespace Zpravodajstvi.Tests.ServicesTests;

public class CommentServiceTests
{
    private readonly Mock<ICommentRepository> _commentRepoMock = new();
    private readonly Mock<IArticleRepository> _articleRepoMock = new();
    private readonly CommentService _service;

    public CommentServiceTests()
    {
        _service = new CommentService(_commentRepoMock.Object, _articleRepoMock.Object);
    }

    [Fact]
    public async Task AddCommentAsync_ValidDtoAndExistingArticle_AddsCommentAndReturnsDto()
    {
        // Arrange
        var dto = new CreateCommentDto
        {
            ArticleId = 1,
            Content = "Velmi zajímavý příspěvek."
        };

        _articleRepoMock
            .Setup(r => r.ExistsAsync(1))
            .ReturnsAsync(true);

        _commentRepoMock
            .Setup(r => r.AddAsync(It.IsAny<Comment>()))
            .ReturnsAsync((Comment c) =>
            {
                c.Id = 10;
                return c;
            });

        // Act
        var result = await _service.AddCommentAsync(dto, "user-1", "Petr Čtenář");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(10, result.Id);
        Assert.Equal("Petr Čtenář", result.AuthorName);
        Assert.Equal("user-1", result.UserId);
        Assert.Equal("Velmi zajímavý příspěvek.", result.Content);
        Assert.Equal(1, result.ArticleId);

        _articleRepoMock.Verify(r => r.ExistsAsync(1), Times.Once);
        _commentRepoMock.Verify(r => r.AddAsync(It.Is<Comment>(c => c.ArticleId == 1 && c.AuthorName == "Petr Čtenář" && c.UserId == "user-1")), Times.Once);
    }

    [Fact]
    public async Task AddCommentAsync_NonExistingArticle_ThrowsKeyNotFoundException()
    {
        // Arrange
        var dto = new CreateCommentDto
        {
            ArticleId = 999,
            Content = "Testovací komentář k neexistujícímu článku."
        };

        _articleRepoMock
            .Setup(r => r.ExistsAsync(999))
            .ReturnsAsync(false);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.AddCommentAsync(dto, "user-1", "Neznámý"));
        Assert.Contains("999", exception.Message);

        _commentRepoMock.Verify(r => r.AddAsync(It.IsAny<Comment>()), Times.Never);
    }

    [Fact]
    public async Task GetCommentsByArticleIdAsync_ReturnsCommentsOrderedByCreatedAtDescending()
    {
        // Arrange
        var comments = new List<Comment>
        {
            new() { Id = 1, ArticleId = 1, AuthorName = "A", Content = "První", CreatedAt = DateTime.UtcNow.AddHours(-2) },
            new() { Id = 2, ArticleId = 1, AuthorName = "B", Content = "Druhý (novější)", CreatedAt = DateTime.UtcNow.AddHours(-1) }
        };

        _commentRepoMock
            .Setup(r => r.GetByArticleIdAsync(1))
            .ReturnsAsync(comments);

        // Act
        var result = (await _service.GetCommentsByArticleIdAsync(1)).ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(2, result[0].Id); // Novější komentář musí být první
        Assert.Equal(1, result[1].Id);
    }

    [Fact]
    public async Task UpdateCommentAsync_Owner_UpdatesContent()
    {
        // Arrange
        var comment = new Comment { Id = 5, ArticleId = 1, AuthorName = "Petr Čtenář", UserId = "user-1", Content = "Původní text" };
        _commentRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(comment);

        // Act
        await _service.UpdateCommentAsync(5, "  Nový text  ", "user-1", isAdmin: false);

        // Assert
        Assert.Equal("Nový text", comment.Content);
        _commentRepoMock.Verify(r => r.UpdateAsync(comment), Times.Once);
    }

    [Fact]
    public async Task UpdateCommentAsync_OtherUserWithSameFullName_ThrowsUnauthorizedAccessException()
    {
        // Arrange – dva různé účty se stejným jménem, vlastník se určuje podle UserId
        var comment = new Comment { Id = 5, ArticleId = 1, AuthorName = "Jan Novák", UserId = "user-1", Content = "Původní text" };
        _commentRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(comment);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _service.UpdateCommentAsync(5, "Cizí úprava", "user-2", isAdmin: false));
        Assert.Equal("Původní text", comment.Content);
        _commentRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Comment>()), Times.Never);
    }

    [Fact]
    public async Task DeleteCommentAsync_Owner_DeletesComment()
    {
        // Arrange
        var comment = new Comment { Id = 5, ArticleId = 1, AuthorName = "Petr Čtenář", UserId = "user-1" };
        _commentRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(comment);

        // Act
        await _service.DeleteCommentAsync(5, "user-1", isAdmin: false);

        // Assert
        _commentRepoMock.Verify(r => r.DeleteAsync(comment), Times.Once);
    }

    [Fact]
    public async Task DeleteCommentAsync_CommentWithoutUserId_OnlyAdminCanDelete()
    {
        // Arrange – starý komentář, u kterého migrace nedokázala jednoznačně určit autora
        var comment = new Comment { Id = 6, ArticleId = 1, AuthorName = "Jan Redaktor", UserId = null };
        _commentRepoMock.Setup(r => r.GetByIdAsync(6)).ReturnsAsync(comment);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _service.DeleteCommentAsync(6, "user-1", isAdmin: false));
        await _service.DeleteCommentAsync(6, "admin-1", isAdmin: true);

        _commentRepoMock.Verify(r => r.DeleteAsync(comment), Times.Once);
    }
}
