using KeeperData.Application.Queries;
using KeeperData.Core.DTOs;
using KeeperData.Core.Exceptions;
using KeeperData.Core.Repositories;

namespace KeeperData.Application.Queries.Species;

public class GetSpeciesByIdQueryHandler(ISpeciesRepository repository) : IQueryHandler<GetSpeciesByIdQuery, SpeciesDTO>
{
    private readonly ISpeciesRepository _repository = repository;

    public async Task<SpeciesDTO> Handle(GetSpeciesByIdQuery request, CancellationToken cancellationToken)
    {
        var species = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException($"Document with Id {request.Id} not found.");

        return new SpeciesDTO
        {
            IdentifierId = species.IdentifierId,
            Code = species.Code,
            Name = species.Name,
            LastUpdatedDate = species.LastModifiedDate
        };
    }
}