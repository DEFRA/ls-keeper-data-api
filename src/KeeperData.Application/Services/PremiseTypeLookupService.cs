using KeeperData.Core.Documents;
using KeeperData.Core.Repositories;
using KeeperData.Core.Services;

namespace KeeperData.Application.Services;

public class PremiseTypeLookupService(IReferenceDataCache cache) : IPremiseTypeLookupService
{
    public Task<PremisesTypeDocument?> GetByIdAsync(string? id, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id))
            return Task.FromResult<PremisesTypeDocument?>(null);

        var match = cache.PremisesTypes.FirstOrDefault(x =>
            x.IdentifierId.Equals(id, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(match);
    }

    public Task<(string? premiseTypeId, string? premiseTypeName)> FindAsync(string? lookupValue, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(lookupValue))
            return Task.FromResult<(string?, string?)>((null, null));

        var match = cache.PremisesTypes.FirstOrDefault(x =>
            x.Code.Equals(lookupValue, StringComparison.OrdinalIgnoreCase) ||
            x.Name.Equals(lookupValue, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(match != null
            ? (match.IdentifierId, match.Name)
            : ((string?)null, (string?)null));
    }
}