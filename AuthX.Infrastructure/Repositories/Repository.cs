using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using AuthX.Core.Interfaces;
using AuthX.Infrastructure.Data;

namespace AuthX.Infrastructure.Repositories;

public class Repository<T> : IRepository<T> where T : class
{
    protected readonly AppDbContext _ctx;
    protected readonly DbSet<T>     _set;

    public Repository(AppDbContext ctx)
    {
        _ctx = ctx;
        _set = ctx.Set<T>();
    }

    public async Task<T?> GetByIdAsync(object id)
        => await _set.FindAsync(id);

    public async Task<IEnumerable<T>> GetAllAsync()
        => await _set.ToListAsync();

    public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
        => await _set.Where(predicate).ToListAsync();

    public async Task<T?> FindOneAsync(Expression<Func<T, bool>> predicate)
        => await _set.FirstOrDefaultAsync(predicate);

    public async Task AddAsync(T entity)
        => await _set.AddAsync(entity);

    public async Task AddRangeAsync(IEnumerable<T> entities)
        => await _set.AddRangeAsync(entities);

    public void Update(T entity)
        => _set.Update(entity);

    public void Remove(T entity)
        => _set.Remove(entity);

    public async Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null)
        => predicate == null
            ? await _set.CountAsync()
            : await _set.CountAsync(predicate);

    public async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate)
        => await _set.AnyAsync(predicate);

    public IQueryable<T> Query()
        => _set.AsQueryable();
}