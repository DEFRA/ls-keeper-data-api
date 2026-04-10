using KeeperData.Application.Queries;
using KeeperData.Core.DTOs;
using KeeperData.Core.Repositories;

namespace KeeperData.Application.Queries.Species;

public class GetSpeciesQueryHandler(ISpeciesRepository repository) : IQueryHandler<GetSpeciesQuery, IEnumerable<SpeciesListResponse>>
{
    private readonly ISpeciesRepository _repository = repository;

    public async Task<IEnumerable<SpeciesListResponse>> Handle(GetSpeciesQuery request, CancellationToken cancellationToken)
    {
        var items = await _repository.GetAllAsync(cancellationToken);

        var filteredItems = items
            .Where(s => !request.LastUpdatedDate.HasValue || s.LastModifiedDate >= request.LastUpdatedDate.Value)
            .OrderBy(s => s.SortOrder) //Sort be sort order first
            .ThenBy(s => s.Name)
            .Select(s => new SpeciesDTO
            {
                IdentifierId = s.IdentifierId,
                Code = s.Code,
                Name = s.Name,
                LastUpdatedDate = s.LastModifiedDate
            })
            .ToList();

        var response = new SpeciesListResponse
        {
            Count = filteredItems.Count,
            Values = filteredItems
        };

        return [response];
    }
}