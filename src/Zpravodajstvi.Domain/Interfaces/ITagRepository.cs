using Zpravodajstvi.Domain.Entities;

namespace Zpravodajstvi.Domain.Interfaces;

public interface ITagRepository : IRepository<Tag>
{
    // Štítky včetně článků (kvůli počtu článků se štítkem)
    Task<IEnumerable<Tag>> GetAllWithArticlesAsync();
    Task<Tag?> GetByIdWithArticlesAsync(int id);
}
