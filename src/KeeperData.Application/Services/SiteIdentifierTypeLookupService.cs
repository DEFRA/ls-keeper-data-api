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

    public async Task<SiteIdentifierTypeDocument?> GetByCodeAsync(string? code, CancellationToken cancellationToken)
    {
        var (siteIdentifierTypeId, _) = await siteIdentifierTypeRepository.FindAsync(code, cancellationToken);
        if (string.IsNullOrWhiteSpace(siteIdentifierTypeId)) return null;
        return await GetByIdAsync(siteIdentifierTypeId, cancellationToken);
    }

    public async Task<(string? siteIdentifierId, string? siteIdentifierName)> FindAsync(string? lookupValue, CancellationToken cancellationToken)
    {
        return await siteIdentifierTypeRepository.FindAsync(lookupValue, cancellationToken);
    }
}