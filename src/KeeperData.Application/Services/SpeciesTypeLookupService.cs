using KeeperData.Core.Documents;
using KeeperData.Core.Repositories;
using KeeperData.Core.Services;

namespace KeeperData.Application.Services;

public class SpeciesTypeLookupService : ISpeciesTypeLookupService
{
    private readonly ISpeciesRepository _speciesRepository;

    public SpeciesTypeLookupService(ISpeciesRepository speciesRepository)
    {
        _speciesRepository = speciesRepository;
    }

    public async Task<SpeciesDocument?> GetByIdAsync(string? id, CancellationToken cancellationToken)
    {
        return await _speciesRepository.GetByIdAsync(id, cancellationToken);
    }

    public async Task<(string? speciesTypeId, string? speciesTypeName)> FindAsync(string? lookupValue, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(lookupValue))
            return (null, null);

        if (lookupValue == "-")
            return (null, null);

        return await _speciesRepository.FindAsync(lookupValue, cancellationToken);
    }
}