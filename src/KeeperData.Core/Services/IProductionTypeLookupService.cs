namespace KeeperData.Core.Services;

public interface IProductionTypeLookupService
{
    Task<(string? productionTypeId, string? productionTypeName)> FindAsync(string? lookupValue, CancellationToken cancellationToken);
}
