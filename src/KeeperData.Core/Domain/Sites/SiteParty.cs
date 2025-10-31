using KeeperData.Core.Domain.BuildingBlocks;

namespace KeeperData.Core.Domain.Sites;

public class SiteParty : ValueObject
{
    public string Id { get; }
    public string? Title { get; private set; }
    public string? FirstName { get; private set; }
    public string? LastName { get; private set; }
    public string? Name { get; private set; }
    public string? CustomerNumber { get; private set; }
    public string? PartyType { get; private set; }
    public IReadOnlyCollection<Communication> Communication { get; private set; }
    public Address? CorrespondanceAddress { get; private set; }
    public IReadOnlyCollection<RolesToParty> PartyRoles { get; private set; }
    public string? State { get; private set; }
    public DateTime? LastUpdatedDate { get; private set; }

    public SiteParty(string id, string? title, string? firstName, string? lastName, string? name, string? customerNumber, string? partyType, IEnumerable<Communication> communication, Address? correspondanceAddress, IEnumerable<RolesToParty> partyRoles, string? state, DateTime? lastUpdatedDate)
    {
        Id = id;
        Title = title;
        FirstName = firstName;
        LastName = lastName;
        Name = name;
        CustomerNumber = customerNumber;
        PartyType = partyType;
        Communication = [.. communication];
        CorrespondanceAddress = correspondanceAddress;
        PartyRoles = [.. partyRoles];
        State = state;
        LastUpdatedDate = lastUpdatedDate;
    }

    public static SiteParty Create(string partyId, string? title, string? firstName, string? lastName, string? name, string? customerNumber, string? partyType, string? state)
    {
        return new SiteParty(
            partyId,
            title,
            firstName,
            lastName,
            name,
            customerNumber,
            partyType,
            [],
            null,
            [],
            state,
            DateTime.UtcNow
        );
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Id;
    }
}