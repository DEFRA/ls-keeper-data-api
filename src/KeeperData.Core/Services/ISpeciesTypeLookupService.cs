namespace KeeperData.Core.Services;

public interface ISpeciesTypeLookupService
{
    Task<(string? speciesTypeId, string? speciesTypeName)> FindAsync(string? lookupValue, CancellationToken cancellationToken);
}
