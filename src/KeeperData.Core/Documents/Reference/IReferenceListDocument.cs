using KeeperData.Core.Repositories;
using System.Collections.Generic;

namespace KeeperData.Core.Documents.Reference;

public interface IReferenceListDocument<TItem> : IListDocument, IEntity
{
    IReadOnlyCollection<TItem> Items { get; }
}
