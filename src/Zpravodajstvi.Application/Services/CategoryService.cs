using Zpravodajstvi.Application.DTOs;
using Zpravodajstvi.Domain.Interfaces;
using Zpravodajstvi.Application.Interfaces;
using Zpravodajstvi.Domain.Entities;

namespace Zpravodajstvi.Application.Services;

public class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _repository;

    public CategoryService(ICategoryRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<CategoryDto>> GetAllCategoriesAsync()
    {
        var categories = await _repository.GetAllWithArticlesAsync();
        return categories.Select(c => new CategoryDto
        {
            Id = c.Id,
            Name = c.Name,
            ArticlesCount = c.Articles?.Count ?? 0
        });
    }

    public async Task<CategoryDto?> GetCategoryByIdAsync(int id)
    {
        var category = await _repository.GetByIdWithArticlesAsync(id);
        if (category == null)
        {
            return null;
        }

        return new CategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            ArticlesCount = category.Articles?.Count ?? 0
        };
    }

    public async Task<CategoryDto> CreateCategoryAsync(string name)
    {
        var category = new Category { Name = name.Trim() };
        var created = await _repository.AddAsync(category);
        return new CategoryDto
        {
            Id = created.Id,
            Name = created.Name,
            ArticlesCount = 0
        };
    }

    public async Task UpdateCategoryAsync(int id, string name)
    {
        var category = await _repository.GetByIdAsync(id);
        if (category == null)
        {
            throw new KeyNotFoundException($"Kategorie s ID {id} nebyla nalezena.");
        }

        category.Name = name.Trim();
        await _repository.UpdateAsync(category);
    }

    public async Task DeleteCategoryAsync(int id)
    {
        var category = await _repository.GetByIdWithArticlesAsync(id);
        if (category == null)
        {
            return;
        }

        // Vazba Category -> Article má DeleteBehavior.Restrict: kategorii s články databáze smazat nedovolí
        // (SQLite Error 19), proto články zkontrolujeme předem a místo chyby 500 vrátíme srozumitelnou hlášku
        var articlesCount = category.Articles?.Count ?? 0;
        if (articlesCount > 0)
        {
            throw new InvalidOperationException(GetCategoryNotEmptyMessage(articlesCount));
        }

        await _repository.DeleteAsync(category);
    }

    // "obsahuje 1 článek / 3 články / 5 článků" – české skloňování podle počtu
    public static string GetCategoryNotEmptyMessage(int articlesCount)
    {
        return articlesCount switch
        {
            1 => "Rubriku nelze smazat, obsahuje 1 článek. Nejprve ho přesuňte nebo smažte.",
            >= 2 and <= 4 => $"Rubriku nelze smazat, obsahuje {articlesCount} články. Nejprve je přesuňte nebo smažte.",
            _ => $"Rubriku nelze smazat, obsahuje {articlesCount} článků. Nejprve je přesuňte nebo smažte."
        };
    }
}
