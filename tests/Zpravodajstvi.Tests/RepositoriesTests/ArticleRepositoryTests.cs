using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Zpravodajstvi.Domain.Entities;
using Zpravodajstvi.Infrastructure.Data;
using Zpravodajstvi.Infrastructure.Repositories;

namespace Zpravodajstvi.Tests.RepositoriesTests;

// Vyhledávání se testuje proti skutečné SQLite databázi v paměti (vytvořené migracemi),
// aby se ověřilo, že nespoléhá na SQL funkci lower(), která neumí česká písmena
public class ArticleRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ApplicationDbContext _context;
    private readonly ArticleRepository _repository;
    private readonly ApplicationUser _author;

    public ArticleRepositoryTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new ApplicationDbContext(options);
        _context.Database.Migrate();

        var category = new Category { Name = "Sport" };
        _author = new ApplicationUser { UserName = "redaktor@test.cz", Email = "redaktor@test.cz", FullName = "Jan Redaktor" };
        _context.Users.Add(_author);
        _context.Articles.AddRange(
            new Article
            {
                Title = "Český hokejový tým vstoupil do šampionátu vítězstvím",
                Perex = "Národní tým získal první body.",
                Content = "Reprezentanti předvedli soustředěný výkon.",
                Category = category,
                Author = _author,
                Tags = new List<Tag> { new() { Name = "Česko" } }
            },
            new Article
            {
                Title = "Revoluce v umělé inteligenci",
                Perex = "Nové modely mění svět vývoje.",
                Content = "Vývojáři získávají mocného parťáka.",
                Category = category
            });
        _context.SaveChanges();
        _context.ChangeTracker.Clear();

        _repository = new ArticleRepository(_context);
    }

    [Theory]
    [InlineData("český")]
    [InlineData("Český")]
    [InlineData("ČESKÝ")]
    [InlineData("cesky")]
    [InlineData("#Česko")]
    [InlineData("#česko")]
    public async Task GetAllAsync_SearchWithCzechCharacters_FindsArticle(string search)
    {
        // Act
        var result = await _repository.GetFilteredAsync(search: search);

        // Assert
        var article = Assert.Single(result);
        Assert.StartsWith("Český hokejový tým", article.Title);
    }

    [Theory]
    [InlineData("Česko")]
    [InlineData("ČESKO")]
    [InlineData("#Česko")]
    public async Task GetAllAsync_TagFilter_IgnoresCaseAndDiacritics(string tag)
    {
        // Act
        var result = await _repository.GetFilteredAsync(tag: tag);

        // Assert
        var article = Assert.Single(result);
        Assert.Contains(article.Tags, t => t.Name == "Česko");
    }

    [Fact]
    public async Task GetFilteredAsync_ByAuthor_ReturnsAuthorsArticlesWithLoadedAuthor()
    {
        // Act
        var result = await _repository.GetFilteredAsync(authorId: _author.Id);

        // Assert – navigační vlastnost Author je načtená přes Include, takže je k dispozici jméno autora
        var article = Assert.Single(result);
        Assert.NotNull(article.Author);
        Assert.Equal("Jan Redaktor", article.Author.FullName);
    }

    [Fact]
    public async Task GetByIdWithDetailsAsync_LoadsAuthorOfArticleAndOfComments()
    {
        // Arrange
        var article = _context.Articles.Single(a => a.AuthorId == _author.Id);
        _context.Comments.Add(new Comment { ArticleId = article.Id, UserId = _author.Id, AuthorName = "Jan Redaktor", Content = "Komentář" });
        _context.SaveChanges();
        _context.ChangeTracker.Clear();

        // Act
        var result = await _repository.GetByIdWithDetailsAsync(article.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Jan Redaktor", result.Author?.FullName);
        Assert.Equal("Jan Redaktor", Assert.Single(result.Comments).User?.FullName);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }
}
