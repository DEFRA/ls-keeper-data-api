using KeeperData.Application.Queries;
using KeeperData.Application.Queries.ReferenceRoles;
using KeeperData.Core.DTOs;
using KeeperData.Core.Services;

namespace KeeperData.Application.Queries.ReferenceRoles;

public class GetReferenceRolesQueryHandler(IReferenceDataCache cache) : IQueryHandler<GetReferenceRolesQuery, RoleListResponse>
{
    private readonly IReferenceDataCache _cache = cache;

    public Task<RoleListResponse> Handle(GetReferenceRolesQuery request, CancellationToken cancellationToken)
    {
        // Get items instantly from memory instead of the database
        var items = _cache.Roles;

        var filteredItems = items
            .Where(r => !request.LastUpdatedDate.HasValue || r.LastModifiedDate >= request.LastUpdatedDate.Value)
            .OrderBy(r => r.SortOrder)
            .ThenBy(r => r.Name)
            .Select(r => new RoleDto
            {
                IdentifierId = r.IdentifierId,
                Code = r.Code,
                Name = r.Name,
                LastUpdatedDate = r.LastModifiedDate
            })
            .ToList();

        var response = new RoleListResponse
        {
            Count = filteredItems.Count,
            Values = filteredItems
        };

        return Task.FromResult(response);
    }
}
