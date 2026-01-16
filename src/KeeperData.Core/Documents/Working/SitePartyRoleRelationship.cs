namespace KeeperData.Core.Documents.Working;

public class SitePartyRoleRelationship
{
    public string? Id { get; set; }

    public required string HoldingIdentifier { get; set; }

    public required string CustomerNumber { get; set; }

    public string? RoleTypeId { get; set; }
}