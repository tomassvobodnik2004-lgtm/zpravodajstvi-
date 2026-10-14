using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Zpravodajstvi.Domain.Entities;
using Zpravodajstvi.Domain.Search;

namespace Zpravodajstvi.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Article> Articles => Set<Article>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<Image> Images => Set<Image>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Konfigurace relace 1:N Category -> Articles
        modelBuilder.Entity<Article>()
            .HasOne(a => a.Category)
            .WithMany(c => c.Articles)
            .HasForeignKey(a => a.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // Konfigurace relace 1:N Article -> Comments
        modelBuilder.Entity<Comment>()
            .HasOne(c => c.Article)
            .WithMany(a => a.Comments)
            .HasForeignKey(c => c.ArticleId)
            .OnDelete(DeleteBehavior.Cascade);

        // Konfigurace relace 1:N ApplicationUser -> Articles (autor článku).
        // Po smazání účtu články zůstanou s AuthorId = null a zobrazí se jako od neznámého autora.
        modelBuilder.Entity<Article>()
            .HasOne(a => a.Author)
            .WithMany(u => u.Articles)
            .HasForeignKey(a => a.AuthorId)
            .OnDelete(DeleteBehavior.SetNull);

        // Konfigurace relace 1:N ApplicationUser -> Comments (autor komentáře).
        // Po smazání účtu komentáře zůstanou s UserId = null a spravovat je může jen administrátor nebo redaktor.
        modelBuilder.Entity<Comment>()
            .HasOne(c => c.User)
            .WithMany(u => u.Comments)
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        // Konfigurace relace 1:N Article -> Images
        modelBuilder.Entity<Image>()
            .HasOne(i => i.Article)
            .WithMany(a => a.Images)
            .HasForeignKey(i => i.ArticleId)
            .OnDelete(DeleteBehavior.Cascade);

        // Konfigurace M:N relace Article <-> Tag
        modelBuilder.Entity<Article>()
            .HasMany(a => a.Tags)
            .WithMany(t => t.Articles);
    }

    // Před každým uložením se doplní normalizovaná pole pro vyhledávání (při vytvoření i úpravě)
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        UpdateSearchFields();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        UpdateSearchFields();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void UpdateSearchFields()
    {
        foreach (var entry in ChangeTracker.Entries<Article>())
        {
            if (entry.State is EntityState.Added or EntityState.Modified)
            {
                entry.Entity.SearchTitle = SearchNormalizer.Normalize(entry.Entity.Title);
                entry.Entity.SearchPerex = SearchNormalizer.Normalize(entry.Entity.Perex);
                entry.Entity.SearchContent = SearchNormalizer.Normalize(entry.Entity.Content);
            }
        }

        foreach (var entry in ChangeTracker.Entries<Tag>())
        {
            if (entry.State is EntityState.Added or EntityState.Modified)
            {
                entry.Entity.SearchName = SearchNormalizer.Normalize(entry.Entity.Name);
            }
        }
    }
}
