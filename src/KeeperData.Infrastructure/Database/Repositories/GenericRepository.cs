using KeeperData.Core.Attributes;
using KeeperData.Core.Repositories;
using KeeperData.Core.Transactions;
using KeeperData.Infrastructure.Database.Configuration;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using System.Linq.Expressions;
using System.Reflection;

namespace KeeperData.Infrastructure.Database.Repositories;

public class GenericRepository<T> : IGenericRepository<T>
    where T : IEntity
{
    protected readonly IMongoCollection<T> _collection;
    private readonly IUnitOfWork _unitOfWork;

    public GenericRepository(
        IOptions<MongoConfig> mongoConfig,
        IMongoClient client,
        IUnitOfWork unitOfWork)
    {
        var mongoDatabase = client.GetDatabase(mongoConfig.Value.DatabaseName);
        var collectionName = typeof(T).GetCustomAttribute<CollectionNameAttribute>()?.Name ?? typeof(T).Name;
        _collection = mongoDatabase.GetCollection<T>(collectionName);
        _unitOfWork = unitOfWork;
    }

    private IClientSessionHandle? Session => _unitOfWork?.Session;

    public async Task<T> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var filter = Builders<T>.Filter.Eq(x => x.Id, id);
        var cursor = await _collection.FindAsync(Session, filter, cancellationToken: cancellationToken);
        return await cursor.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<T?> FindOneAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        var cursor = await _collection.FindAsync(Session, predicate, cancellationToken: cancellationToken);
        return await cursor.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<T?> FindOneByFilterAsync(FilterDefinition<T> filter, CancellationToken cancellationToken = default)
    {
        var cursor = await _collection.FindAsync(Session, filter, cancellationToken: cancellationToken);
        return await cursor.FirstOrDefaultAsync(cancellationToken);
    }

    public Task<List<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default) =>
        _collection.Find(predicate).ToListAsync(cancellationToken);

    public async Task<List<T>> FindAsync<TNested>(
        Expression<Func<T, IEnumerable<TNested>>> arrayField,
        FilterDefinition<TNested> nestedFilter,
        CancellationToken cancellationToken = default)
    {
        var filter = Builders<T>.Filter.ElemMatch(arrayField, nestedFilter);
        return await _collection.Find(filter).ToListAsync(cancellationToken);
    }

    public Task AddAsync(T entity, CancellationToken cancellationToken = default) =>
        _collection.InsertOneAsync(Session, entity, new InsertOneOptions { BypassDocumentValidation = true }, cancellationToken);

    public Task AddManyAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default) =>
        _collection.InsertManyAsync(Session, entities, new InsertManyOptions { BypassDocumentValidation = true }, cancellationToken);

    public Task UpdateAsync(T entity, CancellationToken cancellationToken = default) =>
        _collection.ReplaceOneAsync(Session, x => x.Id == entity.Id, entity, cancellationToken: cancellationToken);

    public Task BulkUpsertAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    {
        var models = entities.Select(entity =>
            new ReplaceOneModel<T>(
                filter: Builders<T>.Filter.Eq(x => x.Id, entity.Id),
                replacement: entity)
            {
                IsUpsert = true
            });

        return _collection.BulkWriteAsync(Session, models.ToList(), cancellationToken: cancellationToken);
    }

    public Task BulkUpsertWithCustomFilterAsync(IEnumerable<(FilterDefinition<T> Filter, T Entity)> items, CancellationToken cancellationToken = default)
    {
        var models = items.Select(item =>
            new ReplaceOneModel<T>(
                filter: item.Filter,
                replacement: item.Entity)
            {
                IsUpsert = true
            });

        return _collection.BulkWriteAsync(Session, models.ToList(), cancellationToken: cancellationToken);
    }

    public Task DeleteAsync(string id, CancellationToken cancellationToken = default) =>
        _collection.DeleteOneAsync(
            session: Session,
            filter: Builders<T>.Filter.Eq(x => x.Id, id),
            cancellationToken: cancellationToken);

    public Task DeleteManyAsync(FilterDefinition<T> filter, CancellationToken cancellationToken = default) =>
        _collection.DeleteManyAsync(
            session: Session,
            filter: filter,
            cancellationToken: cancellationToken);
}