using KeeperData.Application.Queries;
using KeeperData.Core.DTOs;
using KeeperData.Core.Exceptions;
using KeeperData.Core.Repositories;

namespace KeeperData.Application.Queries.Roles;

public class GetRoleByIdQueryHandler(IRoleRepository repository) : IQueryHandler<GetRoleByIdQuery, RoleDto>
{
    private readonly IRoleRepository _repository = repository;

    public async Task<RoleDto> Handle(GetRoleByIdQuery request, CancellationToken cancellationToken)
    {
        var item = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException($"Document with Id {request.Id} not found.");

        return new RoleDto
        {
            IdentifierId = item.IdentifierId,
            Code = item.Code,
            Name = item.Name,
            LastUpdatedDate = item.LastModifiedDate
        };
    }
}