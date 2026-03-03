using KeeperData.Core.Services;

namespace KeeperData.Application.Services;

public class ActivityCodeLookupService(IReferenceDataCache cache) : IActivityCodeLookupService
{
    public Task<(string? premiseType, string? premiseActivityType)> FindByActivityCodeAsync(string? activityCode, CancellationToken cancellationToken)
    {
        if (activityCode == null)
            return Task.FromResult<(string?, string?)>((null, null));

        var result = cache.ActivityMaps.FirstOrDefault(s =>
            s.FacilityActivityCode.Equals(activityCode, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult((result?.AssociatedPremiseTypeCode, result?.AssociatedPremiseActivityCode));
    }
}