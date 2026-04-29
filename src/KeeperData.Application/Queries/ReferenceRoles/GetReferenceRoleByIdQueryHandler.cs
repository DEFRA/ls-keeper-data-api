using KeeperData.Application.Queries;
using KeeperData.Core.DTOs;
using KeeperData.Core.Exceptions;
using KeeperData.Core.Repositories;

namespace KeeperData.Application.Queries.ReferenceRoles;

public class GetReferenceRoleByIdQueryHandler(IRoleRepository repository) : IQueryHandler<GetReferenceRoleByIdQuery, RoleDto>
{
    private readonly IRoleRepository _repository = repository;

    public async Task<RoleDto> Handle(GetReferenceRoleByIdQuery request, CancellationToken cancellationToken)
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
