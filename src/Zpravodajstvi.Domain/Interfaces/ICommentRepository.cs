using Zpravodajstvi.Domain.Entities;

namespace Zpravodajstvi.Domain.Interfaces;

public interface ICommentRepository : IRepository<Comment>
{
    Task<IEnumerable<Comment>> GetByArticleIdAsync(int articleId);
    Task<IEnumerable<Comment>> GetByUserIdAsync(string userId);
    // Všechny komentáře včetně článku a autora (správa komentářů v administraci)
    Task<IEnumerable<Comment>> GetAllWithDetailsAsync();
    Task<Comment?> GetByIdWithDetailsAsync(int id);
}
