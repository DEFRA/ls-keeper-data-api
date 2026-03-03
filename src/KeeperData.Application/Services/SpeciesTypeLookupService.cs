using KeeperData.Core.Documents;
using KeeperData.Core.Repositories;
using KeeperData.Core.Services;

namespace KeeperData.Application.Services;

public class SpeciesTypeLookupService(IReferenceDataCache cache) : ISpeciesTypeLookupService
{
    public Task<SpeciesDocument?> GetByIdAsync(string? id, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id))
            return Task.FromResult<SpeciesDocument?>(null);

        var species = cache.Species.FirstOrDefault(s =>
            s.IdentifierId?.Equals(id, StringComparison.OrdinalIgnoreCase) == true);

        return Task.FromResult(species);
    }

    public Task<(string? speciesTypeId, string? speciesTypeName)> FindAsync(string? lookupValue, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(lookupValue) || lookupValue == "-")
            return Task.FromResult<(string?, string?)>((null, null));

        var species = cache.Species.FirstOrDefault(s =>
            s.Code?.Equals(lookupValue, StringComparison.OrdinalIgnoreCase) == true);

        species ??= cache.Species.FirstOrDefault(s =>
            s.Name?.Equals(lookupValue, StringComparison.OrdinalIgnoreCase) == true);

        return Task.FromResult(species != null
            ? (species.IdentifierId, species.Name)
            : ((string?)null, (string?)null));
    }
}