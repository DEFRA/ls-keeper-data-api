using KeeperData.Core.Documents.Reference;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace KeeperData.Core.Repositories;

public interface IReferenceDataRepository<TDocument, TItem> : IGenericRepository<TDocument>
    where TDocument : class, IReferenceListDocument<TItem>
{
    Task<IReadOnlyCollection<TItem>> GetAllAsync(CancellationToken cancellationToken = default);

    Task RefreshAsync(CancellationToken cancellationToken = default);
}
