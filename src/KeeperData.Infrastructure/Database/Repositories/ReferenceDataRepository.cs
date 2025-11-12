using KeeperData.Core.Documents.Reference;
using KeeperData.Core.Repositories;
using KeeperData.Core.Transactions;
using KeeperData.Infrastructure.Database.Configuration;
using MongoDB.Driver;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace KeeperData.Infrastructure.Database.Repositories;

public abstract class ReferenceDataRepository<TDocument, TItem> : GenericRepository<TDocument>, IReferenceDataRepository<TDocument, TItem>
    where TDocument : class, IReferenceListDocument<TItem>
    where TItem : class, INestedEntity
{
    private readonly Lazy<Task<IReadOnlyCollection<TItem>>> _itemsCache;

    protected ReferenceDataRepository(
        IOptions<MongoConfig> mongoConfig,
        IMongoClient client,
        IUnitOfWork unitOfWork)
        : base(mongoConfig, client, unitOfWork)
    {
        _itemsCache = new Lazy<Task<IReadOnlyCollection<TItem>>>(
            LoadItemsAsync, LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public async Task<IReadOnlyCollection<TItem>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var cachedTask = _itemsCache.Value;

        if (!cachedTask.IsCompleted && cancellationToken.CanBeCanceled)
        {
            return await cachedTask.WaitAsync(cancellationToken).ConfigureAwait(false);
        }

        return await cachedTask.ConfigureAwait(false);
    }

    private async Task<IReadOnlyCollection<TItem>> LoadItemsAsync()
    {
        var documentId = TDocument.DocumentId;
        var referenceDocument = await FindOneAsync(x => x.Id == documentId, CancellationToken.None).ConfigureAwait(false);

        return referenceDocument?.Items ?? Array.Empty<TItem>();
    }
}