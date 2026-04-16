using KeeperData.Application.Queries;
using KeeperData.Core.DTOs;
using KeeperData.Core.Repositories;

namespace KeeperData.Application.Queries.Roles;

public class GetRolesQueryHandler(IRoleRepository repository) : IQueryHandler<GetRolesQuery, IEnumerable<RoleListResponse>>
{
    private readonly IRoleRepository _repository = repository;

    public async Task<IEnumerable<RoleListResponse>> Handle(GetRolesQuery request, CancellationToken cancellationToken)
    {
        var items = await _repository.GetAllAsync(cancellationToken);

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

        return [response];
    }
}