using KeeperData.Core.Documents;
using KeeperData.Core.Services;

namespace KeeperData.Application.Services;

public class SiteIdentifierTypeLookupService(IReferenceDataCache cache)
    : ISiteIdentifierTypeLookupService
{
    public Task<SiteIdentifierTypeDocument?> GetByIdAsync(string? id, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id))
            return Task.FromResult<SiteIdentifierTypeDocument?>(null);

        var match = cache.SiteIdentifierTypes.FirstOrDefault(s =>
            s.IdentifierId.Equals(id, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(match);
    }

    public Task<SiteIdentifierTypeDocument?> GetByCodeAsync(string? code, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(code))
            return Task.FromResult<SiteIdentifierTypeDocument?>(null);

        var match = cache.SiteIdentifierTypes.FirstOrDefault(s =>
            s.Code.Equals(code, StringComparison.OrdinalIgnoreCase));

        match ??= cache.SiteIdentifierTypes.FirstOrDefault(s =>
            s.Name.Equals(code, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(match);
    }

    public Task<(string? siteIdentifierId, string? siteIdentifierName)> FindAsync(string? lookupValue, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(lookupValue))
            return Task.FromResult<(string?, string?)>((null, null));

        var match = cache.SiteIdentifierTypes.FirstOrDefault(s =>
            s.Code.Equals(lookupValue, StringComparison.OrdinalIgnoreCase));

        match ??= cache.SiteIdentifierTypes.FirstOrDefault(s =>
            s.Name.Equals(lookupValue, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(match != null
            ? (match.IdentifierId, match.Name)
            : ((string?)null, (string?)null));
    }
}