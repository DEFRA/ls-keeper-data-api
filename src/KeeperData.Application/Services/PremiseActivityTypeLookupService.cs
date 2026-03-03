using KeeperData.Core.Documents;
using KeeperData.Core.Repositories;
using KeeperData.Core.Services;

namespace KeeperData.Application.Services;

public class PremiseActivityTypeLookupService(IReferenceDataCache cache) : IPremiseActivityTypeLookupService
{
    public Task<PremisesActivityTypeDocument?> GetByIdAsync(string? id, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id))
            return Task.FromResult<PremisesActivityTypeDocument?>(null);

        var match = cache.PremisesActivityTypes.FirstOrDefault(s =>
            s.IdentifierId.Equals(id, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(match);
    }

    public Task<PremisesActivityTypeDocument?> GetByCodeAsync(string? code, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(code))
            return Task.FromResult<PremisesActivityTypeDocument?>(null);

        var match = cache.PremisesActivityTypes.FirstOrDefault(s =>
            s.Code.Equals(code, StringComparison.OrdinalIgnoreCase));

        match ??= cache.PremisesActivityTypes.FirstOrDefault(s =>
            s.Name.Equals(code, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(match);
    }

    public Task<(string? premiseActivityTypeId, string? premiseActivityTypeName)> FindAsync(string? lookupValue, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(lookupValue))
            return Task.FromResult<(string?, string?)>((null, null));

        var match = cache.PremisesActivityTypes.FirstOrDefault(s =>
            s.Code.Equals(lookupValue, StringComparison.OrdinalIgnoreCase));

        match ??= cache.PremisesActivityTypes.FirstOrDefault(s =>
            s.Name.Equals(lookupValue, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(match != null
            ? (match.IdentifierId, match.Name)
            : ((string?)null, (string?)null));
    }
}