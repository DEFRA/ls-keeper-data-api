using KeeperData.Application.Queries;
using KeeperData.Core.Documents;
using KeeperData.Core.DTOs;
using KeeperData.Core.Services;

namespace KeeperData.Application.Queries.ReferenceActivities;

public class GetReferenceActivitiesQueryHandler(IReferenceDataCache cache) : IQueryHandler<GetReferenceActivitiesQuery, ReferenceActivityListResponse>
{
    private readonly IReferenceDataCache _cache = cache;

    public Task<ReferenceActivityListResponse> Handle(GetReferenceActivitiesQuery request, CancellationToken cancellationToken)
    {
        var items = _cache.SiteActivityTypes;

        var filteredItems = items
            .Where(a => !request.LastUpdatedDate.HasValue || a.LastModifiedDate >= request.LastUpdatedDate.Value)
            .OrderBy(a => a.PriorityOrder)
            .ThenBy(a => a.Name)
            .Select(a => new ReferenceActivityDto
            {
                Id = a.IdentifierId,
                Code = a.Code,
                Name = a.Name
            })
            .ToList();

        var response = new ReferenceActivityListResponse
        {
            Count = filteredItems.Count,
            Values = filteredItems
        };

        return Task.FromResult(response);
    }
}