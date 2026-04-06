using KeeperData.Core.Documents;
using KeeperData.Core.Repositories;
using KeeperData.Core.Services;

namespace KeeperData.Application.Services;

public class SiteTypeLookupService(IReferenceDataCache cache) : ISiteTypeLookupService
{
    public Task<SiteTypeDocument?> GetByIdAsync(string? id, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id))
            return Task.FromResult<SiteTypeDocument?>(null);

        var match = cache.SiteTypes.FirstOrDefault(x =>
            x.IdentifierId.Equals(id, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(match);
    }

    public Task<SiteTypeDocument?> GetByCodeAsync(string? code, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(code))
            return Task.FromResult<SiteTypeDocument?>(null);

        var match = cache.SiteTypes.FirstOrDefault(x =>
            x.Code.Equals(code, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(match);
    }

    public Task<(string? siteTypeId, string? siteTypeName)> FindAsync(string? lookupValue, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(lookupValue))
            return Task.FromResult<(string?, string?)>((null, null));

        var match = cache.SiteTypes.FirstOrDefault(x =>
            x.Code.Equals(lookupValue, StringComparison.OrdinalIgnoreCase) ||
            x.Name.Equals(lookupValue, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(match != null
            ? (match.IdentifierId, match.Name)
            : ((string?)null, (string?)null));
    }
}