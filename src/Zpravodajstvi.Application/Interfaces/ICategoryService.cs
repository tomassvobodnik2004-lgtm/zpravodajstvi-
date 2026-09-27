using Zpravodajstvi.Application.DTOs;

namespace Zpravodajstvi.Application.Interfaces;

public interface ICategoryService
{
    Task<IEnumerable<CategoryDto>> GetAllCategoriesAsync();
    Task<CategoryDto?> GetCategoryByIdAsync(int id);
    Task<CategoryDto> CreateCategoryAsync(string name);
    Task DeleteCategoryAsync(int id);
}
