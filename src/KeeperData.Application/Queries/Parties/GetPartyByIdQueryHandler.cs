using KeeperData.Core.Documents;
using KeeperData.Core.DTOs;
using KeeperData.Core.Exceptions;
using KeeperData.Core.Repositories;

namespace KeeperData.Application.Queries.Parties;

public class GetPartyByIdQueryHandler(IGenericRepository<PartyDocument> repository) : IQueryHandler<GetPartyByIdQuery, PartyDto>
{
    public async Task<PartyDto> Handle(GetPartyByIdQuery request, CancellationToken cancellationToken)
    {
        var document = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException($"Document with Id {request.Id} not found.");

        return document.ToDto();
    }
}