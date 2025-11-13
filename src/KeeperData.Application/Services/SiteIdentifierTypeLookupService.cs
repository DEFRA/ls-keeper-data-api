using KeeperData.Core.Documents;
using KeeperData.Core.Repositories;
using KeeperData.Core.Services;

namespace KeeperData.Application.Services;

public class SiteIdentifierTypeLookupService(ISiteIdentifierTypeRepository siteIdentifierTypeRepository)
    : ISiteIdentifierTypeLookupService
{
    public async Task<SiteIdentifierTypeDocument?> GetByIdAsync(string? id, CancellationToken cancellationToken)
    {
        return await siteIdentifierTypeRepository.GetByIdAsync(id, cancellationToken);
    }

    public async Task<(string? siteIdentifierId, string? siteIdentifierName)> FindAsync(string? lookupValue, CancellationToken cancellationToken)
    {
        return await siteIdentifierTypeRepository.FindAsync(lookupValue, cancellationToken);
    }
}