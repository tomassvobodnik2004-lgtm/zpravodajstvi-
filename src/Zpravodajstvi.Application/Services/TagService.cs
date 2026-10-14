using Zpravodajstvi.Application.DTOs;
using Zpravodajstvi.Application.Interfaces;
using Zpravodajstvi.Domain.Entities;
using Zpravodajstvi.Domain.Interfaces;

namespace Zpravodajstvi.Application.Services;

public class TagService : ITagService
{
    private readonly ITagRepository _repository;

    public TagService(ITagRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<TagDto>> GetAllTagsAsync()
    {
        var tags = await _repository.GetAllWithArticlesAsync();
        return tags
            .OrderBy(t => t.Name)
            .Select(ToDto);
    }

    public async Task<TagDto?> GetTagByIdAsync(int id)
    {
        var tag = await _repository.GetByIdWithArticlesAsync(id);
        return tag == null ? null : ToDto(tag);
    }

    public async Task<TagDto> CreateTagAsync(string name)
    {
        var created = await _repository.AddAsync(new Tag { Name = CleanName(name) });
        return ToDto(created);
    }

    public async Task UpdateTagAsync(int id, string name)
    {
        var tag = await _repository.GetByIdAsync(id);
        if (tag == null)
        {
            throw new KeyNotFoundException($"Štítek s ID {id} nebyl nalezen.");
        }

        tag.Name = CleanName(name);
        await _repository.UpdateAsync(tag);
    }

    // Smazáním štítku zmizí jen jeho vazby na články (tabulka ArticleTag), samotné články zůstanou
    public async Task DeleteTagAsync(int id)
    {
        var tag = await _repository.GetByIdAsync(id);
        if (tag != null)
        {
            await _repository.DeleteAsync(tag);
        }
    }

    // Štítek se ukládá bez úvodního "#" (ten se přidává až při zobrazení)
    private static string CleanName(string name)
    {
        return name.Trim().TrimStart('#').Trim();
    }

    private static TagDto ToDto(Tag tag)
    {
        return new TagDto
        {
            Id = tag.Id,
            Name = tag.Name,
            ArticlesCount = tag.Articles?.Count ?? 0
        };
    }
}
