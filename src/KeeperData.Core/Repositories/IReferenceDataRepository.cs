using KeeperData.Core.Documents.Reference;

namespace KeeperData.Core.Repositories;

public interface IReferenceDataRepository<TDocument, TItem> : IGenericRepository<TDocument>
    where TDocument : class, IReferenceListDocument<TItem>
{
    Task<IReadOnlyCollection<TItem>> GetAllAsync(CancellationToken cancellationToken = default);
}