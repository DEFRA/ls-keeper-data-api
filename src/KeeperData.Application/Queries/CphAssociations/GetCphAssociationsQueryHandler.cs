using KeeperData.Application.Configuration;
using KeeperData.Core.Repositories;
using Microsoft.Extensions.Options;

namespace KeeperData.Application.Queries.CphAssociations;

public class GetCphAssociationsQueryHandler(
    ICphAssociationsRepository repository,
    IOptions<UserAccountAssociationConfig> config)
    : IQueryHandler<GetCphAssociationsQuery, List<CphAssociationResult>>
{
    private readonly ICphAssociationsRepository _repository = repository;
    private readonly UserAccountAssociationConfig _config = config.Value;

    public async Task<List<CphAssociationResult>> Handle(GetCphAssociationsQuery request, CancellationToken cancellationToken)
    {
        var roles = _config.Roles ?? ["owner"];

        var associations = await _repository.FindByEmailAsync(request.Email, roles, cancellationToken);

        return associations.Select(a => new CphAssociationResult
        {
            Cph = a.CphNumber,
            Role = a.Role
        }).ToList();
    }
}
