using KeeperData.Core.Documents;
using KeeperData.Core.Services;

namespace KeeperData.Application.Services;

public class SpeciesTypeLookupService : ISpeciesTypeLookupService
{
    /// <summary>
    /// To complete implementation when seeding is completed or to replace.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<SpeciesDocument?> GetByIdAsync(string? id, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;

        return await Task.FromResult(new SpeciesDocument
        {
            IdentifierId = id,
            Code = "ABC",
            Name = "XYZ"
        });
    }

    /// <summary>
    /// To complete implementation when seeding is completed or to replace.
    /// </summary>
    /// <param name="lookupValue"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<(string? speciesTypeId, string? speciesTypeName)> FindAsync(string? lookupValue, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(lookupValue)) return (null, null);

        string? speciesTypeId = null;
        string? speciesTypeName = null;

        return await Task.FromResult((speciesTypeId, speciesTypeName));
    }
}