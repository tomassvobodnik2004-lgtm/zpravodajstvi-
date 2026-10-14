using Zpravodajstvi.Application.DTOs;

namespace Zpravodajstvi.Application.Interfaces;

public interface ICategoryService
{
    Task<IEnumerable<CategoryDto>> GetAllCategoriesAsync();
    Task<CategoryDto?> GetCategoryByIdAsync(int id);
    Task<CategoryDto> CreateCategoryAsync(string name);
    Task UpdateCategoryAsync(int id, string name);
    // Kategorii s články smazat nelze – služba pak vyhodí InvalidOperationException se srozumitelnou hláškou
    Task DeleteCategoryAsync(int id);
}
