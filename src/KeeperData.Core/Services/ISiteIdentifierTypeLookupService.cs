using KeeperData.Core.Documents;

namespace KeeperData.Core.Services;

public interface ISiteIdentifierTypeLookupService
{
    Task<SiteIdentifierTypeDocument?> GetByIdAsync(string? id, CancellationToken cancellationToken);

    Task<SiteIdentifierTypeDocument?> GetByCodeAsync(string? code, CancellationToken cancellationToken);

    Task<(string? siteIdentifierId, string? siteIdentifierName)> FindAsync(string? lookupValue, CancellationToken cancellationToken);
}