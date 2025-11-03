using KeeperData.Core.Documents;
using KeeperData.Core.Services;

namespace KeeperData.Application.Services;

public class PremiseActivityTypeLookupService : IPremiseActivityTypeLookupService
{
    /// <summary>
    /// To complete implementation when seeding is completed or to replace.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<PremisesActivityTypeDocument?> GetByIdAsync(string? id, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;

        return await Task.FromResult(new PremisesActivityTypeDocument
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
    public async Task<(string? premiseActivityTypeId, string? premiseActivityTypeName)> FindAsync(string? lookupValue, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(lookupValue)) return (null, null);

        string? premiseActivityTypeId = null;
        string? premiseActivityTypeName = null;

        return await Task.FromResult((premiseActivityTypeId, premiseActivityTypeName));
    }
}