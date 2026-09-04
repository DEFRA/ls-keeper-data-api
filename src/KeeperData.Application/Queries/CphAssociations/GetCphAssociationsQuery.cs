namespace KeeperData.Application.Queries.CphAssociations;

public class GetCphAssociationsQuery : IQuery<List<CphAssociationResult>>
{
    public required string Email { get; init; }
}
