using KeeperData.Core.Attributes;
using KeeperData.Core.Documents.Reference;
using KeeperData.Core.Repositories;
using KeeperData.Core.Transactions;
using KeeperData.Infrastructure.Database.Configuration;
using MongoDB.Driver;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace KeeperData.Infrastructure.Database.Repositories;

public class ReferenceDataRepository<TDocument, TItem> : GenericRepository<TDocument>, IReferenceDataRepository<TDocument, TItem>
    where TDocument : class, IReferenceListDocument<TItem>
    where TItem : class, INestedEntity
{
    private readonly Lazy<Task<IReadOnlyCollection<TItem>>> _itemsCache;

    public ReferenceDataRepository(
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
        var attribute = typeof(TItem).GetCustomAttribute<ReferenceDataAttribute<TDocument, TItem>>()
            ?? throw new InvalidOperationException(
                $"Type {typeof(TItem).Name} must be decorated with [ReferenceData<{typeof(TDocument).Name}, {typeof(TItem).Name}>]");

        var referenceDocument = await FindOneAsync(x => x.Id == attribute.DocumentId, CancellationToken.None).ConfigureAwait(false);

        if (referenceDocument == null)
        {
            return Array.Empty<TItem>();
        }

        var itemsProperty = typeof(TDocument).GetProperty(attribute.ItemsPropertyName, BindingFlags.IgnoreCase | BindingFlags.Public)
            ?? throw new InvalidOperationException(
                $"Property '{attribute.ItemsPropertyName}' not found on {typeof(TDocument).Name}");

        var items = itemsProperty.GetValue(referenceDocument) as IEnumerable<TItem>;

        if (items == null || !items.Any())
        {
            return Array.Empty<TItem>();
        }

        return items.ToArray();
    }
}
