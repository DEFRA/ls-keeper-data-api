using KeeperData.Core.Repositories;
using KeeperData.Core.Services;

namespace KeeperData.Application.Services;

public class ActivityCodeLookupService(IFacilityBusinessActivityMapRepository facilityRepository) : IActivityCodeLookupService
{
    private readonly IFacilityBusinessActivityMapRepository _facilityRepository = facilityRepository;

    public async Task<(string? premiseType, string? premiseActivityType)> FindByActivityCodeAsync(string? activityCode, CancellationToken cancellationToken = default)
    {
        if (activityCode == null)
            return (null, null);

        var result = await _facilityRepository.FindByActivityCodeAsync(activityCode, cancellationToken);
        return (result?.AssociatedPremiseTypeCode, result?.AssociatedPremiseActivityCode);
    }
}