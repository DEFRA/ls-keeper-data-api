using KeeperData.Application.Queries;
using KeeperData.Core.DTOs;

namespace KeeperData.Application.Queries.Roles;

public class GetRolesQuery : IQuery<IEnumerable<RoleListResponse>>
{
    public DateTime? LastUpdatedDate { get; set; }
}