using KeeperData.Application.Queries;
using KeeperData.Core.DTOs;
using KeeperData.Core.Exceptions;
using KeeperData.Core.Repositories;

namespace KeeperData.Application.Queries.IdentifierTypes;

public class GetIdentifierTypeByIdQueryHandler(ISiteIdentifierTypeRepository repository) : IQueryHandler<GetIdentifierTypeByIdQuery, IdentifierTypeDTO>
{
    private readonly ISiteIdentifierTypeRepository _repository = repository;

    public async Task<IdentifierTypeDTO> Handle(GetIdentifierTypeByIdQuery request, CancellationToken cancellationToken)
    {
        var item = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException($"Document with Id {request.Id} not found.");

        return new IdentifierTypeDTO
        {
            IdentifierId = item.IdentifierId,
            Code = item.Code,
            Name = item.Name,
            Description = null, // TODO: We are not pulling this data at the moment
            LastUpdatedDate = item.LastModifiedDate
        };
    }
}