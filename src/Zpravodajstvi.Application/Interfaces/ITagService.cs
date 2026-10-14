using Zpravodajstvi.Application.DTOs;

namespace Zpravodajstvi.Application.Interfaces;

public interface ITagService
{
    Task<IEnumerable<TagDto>> GetAllTagsAsync();
    Task<TagDto?> GetTagByIdAsync(int id);
    Task<TagDto> CreateTagAsync(string name);
    Task UpdateTagAsync(int id, string name);
    Task DeleteTagAsync(int id);
}
