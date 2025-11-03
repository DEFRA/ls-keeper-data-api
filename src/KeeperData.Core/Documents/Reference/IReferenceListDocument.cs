using KeeperData.Core.Repositories;
using System.Collections.Generic;

namespace KeeperData.Core.Documents.Reference;

public interface IReferenceListDocument<TItem> : IEntity
{
    new string Id { get; set; }
    DateTime LastUpdatedDate { get; set; }
    IReadOnlyCollection<TItem> Items { get; }
}
