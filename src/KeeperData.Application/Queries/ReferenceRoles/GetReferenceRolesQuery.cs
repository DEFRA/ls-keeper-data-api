using KeeperData.Application.Queries;
using KeeperData.Core.DTOs;

namespace KeeperData.Application.Queries.ReferenceRoles;

public class GetReferenceRolesQuery : IQuery<RoleListResponse>
{
    public DateTime? LastUpdatedDate { get; set; }
}