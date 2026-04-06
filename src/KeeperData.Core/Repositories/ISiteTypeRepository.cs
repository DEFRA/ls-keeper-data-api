using KeeperData.Core.Documents;

namespace KeeperData.Core.Repositories;

public interface ISiteTypeRepository
{
    Task<SiteTypeDocument?> GetByIdAsync(string? id, CancellationToken cancellationToken = default);
    Task<(string? siteTypeId, string? siteTypeName)> FindAsync(string? lookupValue, CancellationToken cancellationToken = default);
}