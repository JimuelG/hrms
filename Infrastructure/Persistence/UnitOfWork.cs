using System.Collections.Concurrent;
using Core.Common;
using Core.Entities;
using Core.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;
public class UnitOfWork(AppDbContext context) : IUnitOfWork
{
    private readonly ConcurrentDictionary<string, object> _repositories = new();
    public async Task<bool> Complete()
    {
        try
        {
            return await context.SaveChangesAsync() > 0;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            context.ChangeTracker.Clear();
            throw new ConcurrencyConflictException("The record was changed by someone else.", ex);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        {
            context.ChangeTracker.Clear();
            throw new DuplicateKeyException("A unique constraint rejected the change.", ex);
        }
    }
    public void Dispose()
    {
        context.Dispose();
    }
    public IGenericRepository<TEntity> Repository<TEntity>() where TEntity : BaseEntity
    {
        var type = typeof(TEntity).Name;

        return (IGenericRepository<TEntity>)_repositories.GetOrAdd(type, t =>
        {
            var repositoryType = typeof(GenericRepository<>).MakeGenericType(typeof(TEntity));
            return Activator.CreateInstance(repositoryType, context) 
                ?? throw new InvalidOperationException(
                    $"Count not create repository instnace for {t}"
                );
        });
    }
}