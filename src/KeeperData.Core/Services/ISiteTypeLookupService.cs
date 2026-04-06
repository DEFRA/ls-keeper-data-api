using KeeperData.Core.Documents;

namespace KeeperData.Core.Services;

public interface ISiteTypeLookupService
{
    Task<SiteTypeDocument?> GetByIdAsync(string? id, CancellationToken cancellationToken);

    Task<SiteTypeDocument?> GetByCodeAsync(string? code, CancellationToken cancellationToken);

    Task<(string? siteTypeId, string? siteTypeName)> FindAsync(string? lookupValue, CancellationToken cancellationToken);
}