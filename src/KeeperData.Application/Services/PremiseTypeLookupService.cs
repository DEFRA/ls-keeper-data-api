using KeeperData.Core.Services;

namespace KeeperData.Application.Services;

public class PremiseTypeLookupService : IPremiseTypeLookupService
{
    /// <summary>
    /// To complete implementation when seeding is completed or to replace.
    /// </summary>
    /// <param name="lookupValue"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<(string? premiseTypeId, string? premiseTypeName)> FindAsync(string? lookupValue, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(lookupValue)) return (null, null);

        string? premiseTypeId = null;
        string? premiseTypeName = null;

        return await Task.FromResult((premiseTypeId, premiseTypeName));
    }
}
