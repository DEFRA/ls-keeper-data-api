using KeeperData.Core.Documents;
using KeeperData.Core.Repositories;
using KeeperData.Core.Services;

namespace KeeperData.Application.Services;

public class PremiseTypeLookupService(IPremisesTypeRepository premisesTypeRepository) : IPremiseTypeLookupService
{
    public Task<PremisesTypeDocument?> GetByIdAsync(string? id, CancellationToken cancellationToken) =>
        premisesTypeRepository.GetByIdAsync(id, cancellationToken);

    public Task<(string? premiseTypeId, string? premiseTypeName)> FindAsync(string? lookupValue, CancellationToken cancellationToken) =>
        premisesTypeRepository.FindAsync(lookupValue, cancellationToken);
}