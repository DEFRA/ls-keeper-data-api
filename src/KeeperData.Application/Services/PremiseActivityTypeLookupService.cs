using KeeperData.Core.Services;

namespace KeeperData.Application.Services;

public class PremiseActivityTypeLookupService : IPremiseActivityTypeLookupService
{
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
