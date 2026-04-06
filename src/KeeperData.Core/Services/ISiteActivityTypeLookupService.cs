using KeeperData.Core.Documents;

namespace KeeperData.Core.Services;

public interface ISiteActivityTypeLookupService
{
    Task<SiteActivityTypeDocument?> GetByIdAsync(string? id, CancellationToken cancellationToken);

    Task<SiteActivityTypeDocument?> GetByCodeAsync(string? code, CancellationToken cancellationToken);

    Task<(string? siteActivityTypeId, string? siteActivityTypeName)> FindAsync(string? lookupValue, CancellationToken cancellationToken);
}