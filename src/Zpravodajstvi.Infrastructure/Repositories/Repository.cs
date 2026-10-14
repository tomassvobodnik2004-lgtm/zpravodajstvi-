using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Zpravodajstvi.Domain.Interfaces;
using Zpravodajstvi.Infrastructure.Data;

namespace Zpravodajstvi.Infrastructure.Repositories;

// Obecná implementace IRepository<T> nad EF Core. Specifické repozitáře z ní dědí
// a přidávají dotazy s Include (načtení navigačních vlastností).
public class Repository<T> : IRepository<T> where T : class
{
    protected readonly ApplicationDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public Repository(ApplicationDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public virtual async Task<T?> GetByIdAsync(int id)
    {
        return await _dbSet.FindAsync(id);
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync()
    {
        return await _dbSet.AsNoTracking().ToListAsync();
    }

    public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
    {
        return await _dbSet.Where(predicate).ToListAsync();
    }

    public virtual async Task<T> AddAsync(T entity)
    {
        await _dbSet.AddAsync(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public virtual async Task UpdateAsync(T entity)
    {
        // Entitu načtenou tímto kontextem EF Core sleduje a změny najde sám,
        // nesledovanou (např. sestavenou z formuláře) je potřeba nejdřív připojit
        if (_context.Entry(entity).State == EntityState.Detached)
        {
            _dbSet.Update(entity);
        }

        await _context.SaveChangesAsync();
    }

    public virtual async Task DeleteAsync(T entity)
    {
        _dbSet.Remove(entity);
        await _context.SaveChangesAsync();
    }
}
