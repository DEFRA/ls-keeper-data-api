namespace KeeperData.Core.Entities.ReadModel;

/// <summary>
/// A party in the normalised SAM read model.
/// </summary>
public class PartyEntity
{
    public string Id { get; set; } = string.Empty;
    public string SourcePartyId { get; set; } = string.Empty;
    public string? GivenName { get; set; }
    public string? FamilyName { get; set; }
    public string? OrganisationName { get; set; }
    public string? Email { get; set; }
}
