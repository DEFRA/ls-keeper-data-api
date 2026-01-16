using MongoDB.Driver;
using System.Linq.Expressions;

namespace KeeperData.Core.Repositories;

public interface IGenericRepository<T> where T : IEntity
{
    Task<T> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<T?> FindOneAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
    Task<T?> FindOneByFilterAsync(FilterDefinition<T> filter, CancellationToken cancellationToken = default);
    Task<List<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

    Task AddAsync(T entity, CancellationToken cancellationToken = default);
    Task AddManyAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);

    Task UpdateAsync(T entity, CancellationToken cancellationToken = default);
    Task BulkUpdateWithCustomFilterAsync(IEnumerable<(FilterDefinition<T> Filter, UpdateDefinition<T> Update)> items, CancellationToken cancellationToken = default);

    Task BulkUpsertWithCustomFilterAsync(IEnumerable<(FilterDefinition<T> Filter, T Entity)> items, CancellationToken cancellationToken = default);

    Task DeleteManyAsync(FilterDefinition<T> filter, CancellationToken cancellationToken = default);
}