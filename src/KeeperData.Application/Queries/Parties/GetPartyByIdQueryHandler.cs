using KeeperData.Core.Documents;
using KeeperData.Core.Exceptions;
using KeeperData.Core.Repositories;

namespace KeeperData.Application.Queries.Parties;

public class GetPartyByIdQueryHandler(IGenericRepository<PartyDocument> repository) : IQueryHandler<GetPartyByIdQuery, PartyDocument>
{
    private readonly IGenericRepository<PartyDocument> _repository = repository;

    public async Task<PartyDocument> Handle(GetPartyByIdQuery request, CancellationToken cancellationToken)
    {
        var document = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException($"Document with Id {request.Id} not found.");

        return document;
    }
}