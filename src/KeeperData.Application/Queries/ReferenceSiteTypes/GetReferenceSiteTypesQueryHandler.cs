using KeeperData.Core.DTOs;
using KeeperData.Core.Services;

namespace KeeperData.Application.Queries.ReferenceSiteTypes;

public class GetReferenceSiteTypesQueryHandler(IReferenceDataCache cache) : IQueryHandler<GetReferenceSiteTypesQuery, ReferenceSiteTypeListResponse>
{
    private readonly IReferenceDataCache _cache = cache;

    public Task<ReferenceSiteTypeListResponse> Handle(GetReferenceSiteTypesQuery request, CancellationToken cancellationToken)
    {
        var items = _cache.SiteTypes;

        var filteredItems = items
            .Where(p => !request.LastUpdatedDate.HasValue || p.LastModifiedDate >= request.LastUpdatedDate.Value)
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.Name)
            .Select(p => new ReferenceSiteTypeDto
            {
                Id = p.IdentifierId,
                Code = p.Code,
                Name = p.Name
            })
            .ToList();

        var response = new ReferenceSiteTypeListResponse
        {
            Count = filteredItems.Count,
            Values = filteredItems
        };

        return Task.FromResult(response);
    }
}