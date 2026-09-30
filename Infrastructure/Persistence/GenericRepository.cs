using System.Linq.Expressions;
using Core.Entities;
using Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public class GenericRepository<T>(AppDbContext context)
    : IGenericRepository<T> where T : BaseEntity
{
    public void Add(T entity)
    {
        context.Set<T>().Add(entity);
    }

    public async Task AddRangeAsync(IEnumerable<T> entities)
    {
        await context.Set<T>().AddRangeAsync(entities);
    }

    public async Task<int> CountAsync(ISpecification<T> spec, CancellationToken ct = default)
    {
        var query = context.Set<T>().AsQueryable();
        query = spec.ApplyCriteria(query);
        
        return await query.CountAsync(ct);
    }

    public Task<bool> ExistsAsync(Guid id, CancellationToken ct = default)
    {
        return context.Set<T>().AnyAsync(x => x.Id == id, ct);
    }

    public async Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await context.Set<T>().FindAsync(id, ct);
    }

    public async Task<T?> GetEntityWithSpec(ISpecification<T> spec, CancellationToken ct = default)
    {
        return await ApplySpecification(spec).FirstOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyList<T>> ListAsync(ISpecification<T> spec, CancellationToken ct = default)
    {
        return await ApplySpecification(spec).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<TResult>> ListAsync<TResult>(ISpecification<T> spec, Expression<Func<T, TResult>> selector, CancellationToken ct = default)
    {
        return await ApplySpecification(spec).Select(selector).ToListAsync(ct);
    }

    public void Remove(T entity)
    {
        context.Set<T>().Remove(entity);
    }

    public void RemoveRange(IEnumerable<T> entities)
    {
        context.Set<T>().RemoveRange(entities);
    }

    private IQueryable<T> ApplySpecification(ISpecification<T> spec)
    {
        return SpecificationEvaluator<T>.GetQuery(context.Set<T>().AsQueryable(), spec);
    }

}