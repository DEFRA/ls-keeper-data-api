using KeeperData.Core.Documents;
using KeeperData.Core.Repositories;
using KeeperData.Core.Services;

namespace KeeperData.Application.Services;

public class PremiseActivityTypeLookupService(IPremisesActivityTypeRepository premisesActivityTypeRepository) : IPremiseActivityTypeLookupService
{
    public Task<PremisesActivityTypeDocument?> GetByIdAsync(string? id, CancellationToken cancellationToken) =>
        premisesActivityTypeRepository.GetByIdAsync(id, cancellationToken);

    public async Task<PremisesActivityTypeDocument?> GetByCodeAsync(string? code, CancellationToken cancellationToken)
    {
        var (premiseActivityTypeId, _) = await premisesActivityTypeRepository.FindAsync(code, cancellationToken);
        if (string.IsNullOrWhiteSpace(premiseActivityTypeId)) return null;
        return await GetByIdAsync(premiseActivityTypeId, cancellationToken);
    }

    public Task<(string? premiseActivityTypeId, string? premiseActivityTypeName)> FindAsync(string? lookupValue, CancellationToken cancellationToken) =>
        premisesActivityTypeRepository.FindAsync(lookupValue, cancellationToken);
}