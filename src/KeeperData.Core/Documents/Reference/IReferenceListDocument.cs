using KeeperData.Core.Repositories;
using System.Collections.Generic;

namespace KeeperData.Core.Documents.Reference;

public interface IReferenceListDocument<TItem> : IEntity
{
    // Document ID for this reference list (e.g., "all-countries").
    // Must be implemented as a static property on the concrete type.
    static abstract string DocumentId { get; }

    new string Id { get; set; }
    DateTime LastUpdatedDate { get; set; }
    IReadOnlyCollection<TItem> Items { get; }
}