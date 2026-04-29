using KeeperData.Application.Queries;
using KeeperData.Core.DTOs;

namespace KeeperData.Application.Queries.ReferenceRoles;

public record GetReferenceRoleByIdQuery(string Id) : IQuery<RoleDto>;
