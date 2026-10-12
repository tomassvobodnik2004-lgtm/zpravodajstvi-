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
        _context.Articles.AddRange(
            new Article
            {
                Title = "Český hokejový tým vstoupil do šampionátu vítězstvím",
                Perex = "Národní tým získal první body.",
                Content = "Reprezentanti předvedli soustředěný výkon.",
                Category = category,
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
        var result = await _repository.GetAllAsync(search: search);

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
        var result = await _repository.GetAllAsync(tag: tag);

        // Assert
        var article = Assert.Single(result);
        Assert.Contains(article.Tags, t => t.Name == "Česko");
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }
}
