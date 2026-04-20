using KeeperData.Application.Queries;
using KeeperData.Core.DTOs;

namespace KeeperData.Application.Queries.Roles;

public class GetRolesQuery : IQuery<RoleListResponse>
{
    public DateTime? LastUpdatedDate { get; set; }
}