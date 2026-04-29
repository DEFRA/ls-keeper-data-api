using KeeperData.Core.Documents;

namespace KeeperData.Core.Repositories;

public interface ISiteActivityTypeRepository
{
    Task<SiteActivityTypeDocument?> GetByIdAsync(string? id, CancellationToken cancellationToken = default);

    Task<(string? siteActivityTypeId, string? siteActivityTypeName)> FindAsync(string? lookupValue, CancellationToken cancellationToken = default);
}