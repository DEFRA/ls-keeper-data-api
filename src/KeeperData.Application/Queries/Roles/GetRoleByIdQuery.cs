using KeeperData.Application.Queries;
using KeeperData.Core.DTOs;

namespace KeeperData.Application.Queries.Roles;

public record GetRoleByIdQuery(string Id) : IQuery<RoleDto>;