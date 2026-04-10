using KeeperData.Application.Queries;
using KeeperData.Core.DTOs;
using KeeperData.Core.Repositories;

namespace KeeperData.Application.Queries.IdentifierTypes;

public class GetIdentifierTypesQueryHandler(ISiteIdentifierTypeRepository repository) : IQueryHandler<GetIdentifierTypesQuery, IEnumerable<IdentifierTypeListResponse>>
{
    private readonly ISiteIdentifierTypeRepository _repository = repository;

    public async Task<IEnumerable<IdentifierTypeListResponse>> Handle(GetIdentifierTypesQuery request, CancellationToken cancellationToken)
    {
        var items = await _repository.GetAllAsync(cancellationToken);

        var filteredItems = items
            .Where(i => !request.LastUpdatedDate.HasValue || i.LastModifiedDate >= request.LastUpdatedDate.Value)
            .OrderBy(i => i.Name)
            .Select(i => new IdentifierTypeDTO
            {
                IdentifierId = i.IdentifierId,
                Code = i.Code,
                Name = i.Name,
                Description = null, // TODO: We are not pulling this data at the moment
                LastUpdatedDate = i.LastModifiedDate
            })
            .ToList();

        var response = new IdentifierTypeListResponse
        {
            Count = filteredItems.Count,
            Values = filteredItems
        };

        return [response];
    }
}