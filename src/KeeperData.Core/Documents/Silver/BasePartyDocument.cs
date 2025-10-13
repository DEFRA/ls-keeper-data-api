namespace KeeperData.Core.Documents.Silver;

public class BasePartyDocument
{
    public string PartyId { get; set; } = string.Empty;
    public string PartyTypeId { get; set; } = string.Empty;

    public string? PartyFullName { get; set; }

    public string? PartyTitleTypeIdentifier { get; set; }
    public string? PartyFirstName { get; set; }
    public string? PartyLastName { get; set; }

    public AddressDocument? Address { get; set; }
    public CommunicationDocument? Communication { get; set; }
    public List<PartyRoleDocument>? Roles { get; set; } = [];
}