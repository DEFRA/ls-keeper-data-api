using KeeperData.Core.Documents.Reference;
using KeeperData.Core.Repositories;
using KeeperData.Core.Transactions;
using KeeperData.Infrastructure.Database.Configuration;
using MongoDB.Driver;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace KeeperData.Infrastructure.Database.Repositories;

public class ReferenceDataRepository<TDocument, TItem> : GenericRepository<TDocument>, IReferenceDataRepository<TDocument, TItem>
    where TDocument : class, IReferenceListDocument<TItem>
{
    private readonly string _documentId;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private Lazy<Task<IReadOnlyCollection<TItem>>> _itemsCache;

    public ReferenceDataRepository(
        IOptions<MongoConfig> mongoConfig,
        IMongoClient client,
        IUnitOfWork unitOfWork,
        string documentId)
        : base(mongoConfig, client, unitOfWork)
    {
        if (string.IsNullOrWhiteSpace(documentId))
        {
            throw new ArgumentException("Document identifier must be provided", nameof(documentId));
        }

        _documentId = documentId;
        _itemsCache = CreateLazyCache();
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

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        await _refreshLock.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            _itemsCache = CreateLazyCache();
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private Lazy<Task<IReadOnlyCollection<TItem>>> CreateLazyCache() =>
        new(() => LoadItemsAsync(CancellationToken.None), LazyThreadSafetyMode.ExecutionAndPublication);

    private async Task<IReadOnlyCollection<TItem>> LoadItemsAsync(CancellationToken cancellationToken)
    {
        var referenceDocument = await FindOneAsync(x => x.Id == _documentId, cancellationToken).ConfigureAwait(false);

        if (referenceDocument?.Items == null || referenceDocument.Items.Count == 0)
        {
            return Array.Empty<TItem>();
        }

        if (referenceDocument.Items is IReadOnlyCollection<TItem> readOnly)
        {
            return readOnly;
        }

        return referenceDocument.Items.ToArray();
    }
}
