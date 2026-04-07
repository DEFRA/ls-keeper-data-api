using KeeperData.Core.Documents;
using KeeperData.Core.Repositories;
using KeeperData.Core.Services;

namespace KeeperData.Application.Services;

public class SiteActivityTypeLookupService(IReferenceDataCache cache) : ISiteActivityTypeLookupService
{
    public Task<SiteActivityTypeDocument?> GetByIdAsync(string? id, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id))
            return Task.FromResult<SiteActivityTypeDocument?>(null);

        var match = cache.SiteActivityTypes.FirstOrDefault(s =>
            s.IdentifierId.Equals(id, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(match);
    }

    public Task<SiteActivityTypeDocument?> GetByCodeAsync(string? code, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(code))
            return Task.FromResult<SiteActivityTypeDocument?>(null);

        var match = cache.SiteActivityTypes.FirstOrDefault(s =>
            s.Code.Equals(code, StringComparison.OrdinalIgnoreCase));

        match ??= cache.SiteActivityTypes.FirstOrDefault(s =>
            s.Name.Equals(code, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(match);
    }

    public Task<(string? siteActivityTypeId, string? siteActivityTypeName)> FindAsync(string? lookupValue, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(lookupValue))
            return Task.FromResult<(string?, string?)>((null, null));

        var match = cache.SiteActivityTypes.FirstOrDefault(s =>
            s.Code.Equals(lookupValue, StringComparison.OrdinalIgnoreCase));

        match ??= cache.SiteActivityTypes.FirstOrDefault(s =>
            s.Name.Equals(lookupValue, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(match != null
            ? (match.IdentifierId, match.Name)
            : ((string?)null, (string?)null));
    }
}