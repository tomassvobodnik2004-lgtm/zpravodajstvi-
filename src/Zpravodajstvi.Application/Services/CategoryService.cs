using Zpravodajstvi.Application.DTOs;
using Zpravodajstvi.Application.Interfaces;
using Zpravodajstvi.Domain.Entities;

namespace Zpravodajstvi.Application.Services;

public class CategoryService : ICategoryService
{
    private readonly IArticleRepository _repository;

    public CategoryService(IArticleRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<CategoryDto>> GetAllCategoriesAsync()
    {
        var categories = await _repository.GetCategoriesAsync();
        return categories.Select(c => new CategoryDto
        {
            Id = c.Id,
            Name = c.Name,
            ArticlesCount = c.Articles?.Count ?? 0
        });
    }

    public async Task<CategoryDto?> GetCategoryByIdAsync(int id)
    {
        var category = await _repository.GetCategoryByIdAsync(id);
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
        var category = new Category { Name = name };
        var created = await _repository.AddCategoryAsync(category);
        return new CategoryDto
        {
            Id = created.Id,
            Name = created.Name,
            ArticlesCount = 0
        };
    }

    public async Task DeleteCategoryAsync(int id)
    {
        await _repository.DeleteCategoryAsync(id);
    }
}
