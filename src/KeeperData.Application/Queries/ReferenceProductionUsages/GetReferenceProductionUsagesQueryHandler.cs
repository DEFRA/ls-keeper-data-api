using KeeperData.Application.Queries;
using KeeperData.Core.Documents;
using KeeperData.Core.DTOs;
using KeeperData.Core.Services;

namespace KeeperData.Application.Queries.ReferenceProductionUsages;

public class GetReferenceProductionUsagesQueryHandler(IReferenceDataCache cache) : IQueryHandler<GetReferenceProductionUsagesQuery, ReferenceProductionUsageListResponse>
{
    private readonly IReferenceDataCache _cache = cache;

    public Task<ReferenceProductionUsageListResponse> Handle(GetReferenceProductionUsagesQuery request, CancellationToken cancellationToken)
    {
        var items = _cache.ProductionUsages;

        var filteredItems = items
            .Where(p => !request.LastUpdatedDate.HasValue || p.LastModifiedDate >= request.LastUpdatedDate.Value)
            .OrderBy(p => p.Description)
            .ThenBy(p => p.Code)
            .Select(p => new ReferenceProductionUsageDto
            {
                Id = p.IdentifierId,
                Code = p.Code,
                Description = p.Description
            })
            .ToList();

        var response = new ReferenceProductionUsageListResponse
        {
            Count = filteredItems.Count,
            Values = filteredItems
        };

        return Task.FromResult(response);
    }
}