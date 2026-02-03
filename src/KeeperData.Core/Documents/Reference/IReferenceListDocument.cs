using KeeperData.Core.Repositories;

namespace KeeperData.Core.Documents.Reference;

public interface IReferenceListDocument<out TItem> : IEntity
{
    // Document ID for this reference list (e.g., "all-countries").
    // Must be implemented as a static property on the concrete type.
    static abstract string DocumentId { get; }

    new string Id { get; set; }
    DateTime LastUpdatedDate { get; set; }
    IReadOnlyCollection<TItem> Items { get; }
}