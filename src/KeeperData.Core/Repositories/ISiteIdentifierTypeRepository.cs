using KeeperData.Core.Documents;
using KeeperData.Core.Documents.Reference;

namespace KeeperData.Core.Repositories;

public interface ISiteIdentifierTypeRepository : IReferenceDataRepository<SiteIdentifierTypeListDocument, SiteIdentifierTypeDocument>
{
    new Task<SiteIdentifierTypeDocument?> GetByIdAsync(string? id, CancellationToken cancellationToken = default);
    Task<(string? siteIdentifierTypeId, string? siteIdentifierTypeName)> FindAsync(string? lookupValue, CancellationToken cancellationToken = default);
}