using KeeperData.Core.Domain.BuildingBlocks;
using KeeperData.Core.Domain.Shared;

namespace KeeperData.Core.Domain.Sites;

public class SiteParty : ValueObject
{
    public string Id { get; private set; }
    public string PartyId { get; private set; }
    public string? Title { get; private set; }
    public string? FirstName { get; private set; }
    public string? LastName { get; private set; }
    public string? Name { get; private set; }
    public string? PartyType { get; private set; }
    public string? State { get; private set; }
    public DateTime CreatedDate { get; private set; }
    public DateTime LastUpdatedDate { get; private set; }
    public Address? CorrespondanceAddress { get; private set; }
    public List<Communication> Communication { get; private set; } = [];
    public List<PartyRole> PartyRoles { get; private set; } = [];

    public SiteParty(
        string id,
        DateTime createdDate,
        DateTime lastUpdatedDate,
        string partyId,
        string? title,
        string? firstName,
        string? lastName,
        string? name,
        string? partyType,
        string? state,
        Address? correspondanceAddress,
        IEnumerable<Communication>? communication,
        IEnumerable<PartyRole>? partyRole)
    {
        Id = id;
        CreatedDate = createdDate;
        LastUpdatedDate = lastUpdatedDate;
        PartyId = partyId;
        Title = title;
        FirstName = firstName;
        LastName = lastName;
        Name = name;
        PartyType = partyType;
        State = state;
        CorrespondanceAddress = correspondanceAddress;
        Communication = communication?.ToList() ?? [];
        PartyRoles = partyRole?.ToList() ?? [];
    }

    public static SiteParty Create(
        string partyId,
        string? title,
        string? firstName,
        string? lastName,
        string? name,
        string? partyType,
        string? state,
        Address? correspondanceAddress,
        IEnumerable<Communication>? communication,
        IEnumerable<PartyRole>? partyRole)
    {
        return new SiteParty(
            Guid.NewGuid().ToString(),
            DateTime.UtcNow,
            DateTime.UtcNow,
            partyId,
            title,
            firstName,
            lastName,
            name,
            partyType,
            state,
            correspondanceAddress,
            communication,
            partyRole);
    }

    public bool ApplyChanges(
        DateTime lastUpdatedDate,
        string partyId,
        string? title,
        string? firstName,
        string? lastName,
        string? name,
        string? partyType,
        string? state,
        Address? correspondanceAddress,
        IEnumerable<Communication>? communication,
        IEnumerable<PartyRole>? partyRole)
    {
        var changed = false;

        changed |= Change(PartyId, partyId, v => PartyId = v, lastUpdatedDate);
        changed |= Change(Title, title, v => Title = v, lastUpdatedDate);
        changed |= Change(FirstName, firstName, v => FirstName = v, lastUpdatedDate);
        changed |= Change(LastName, lastName, v => LastName = v, lastUpdatedDate);
        changed |= Change(Name, name, v => Name = v, lastUpdatedDate);
        changed |= Change(PartyType, partyType, v => PartyType = v, lastUpdatedDate);
        changed |= Change(State, state, v => State = v, lastUpdatedDate);
        changed |= Change(CorrespondanceAddress, correspondanceAddress, v => CorrespondanceAddress = v, lastUpdatedDate);

        var newComm = communication?.ToList() ?? [];
        var newRoles = partyRole?.ToList() ?? [];

        if (!Communication.SequenceEqual(newComm))
        {
            Communication = newComm;
            LastUpdatedDate = lastUpdatedDate;
            changed = true;
        }

        if (!PartyRoles.SequenceEqual(newRoles))
        {
            PartyRoles = newRoles;
            LastUpdatedDate = lastUpdatedDate;
            changed = true;
        }

        return changed;
    }

    private bool Change<T>(T currentValue, T newValue, Action<T> setter, DateTime lastUpdatedAt)
    {
        if (EqualityComparer<T>.Default.Equals(currentValue, newValue)) return false;
        setter(newValue);
        LastUpdatedDate = lastUpdatedAt;
        return true;
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return PartyId;
        yield return Title ?? string.Empty;
        yield return FirstName ?? string.Empty;
        yield return LastName ?? string.Empty;
        yield return Name ?? string.Empty;
        yield return PartyType ?? string.Empty;
        yield return State ?? string.Empty;

        if (CorrespondanceAddress is not null)
        {
            foreach (var component in CorrespondanceAddress.GetEqualityComponents())
                yield return component;
        }
        else
        {
            yield return string.Empty;
        }

        foreach (var comm in Communication.OrderBy(c => c.GetHashCode()))
        {
            foreach (var component in comm.GetEqualityComponents())
                yield return component;
        }

        foreach (var role in PartyRoles.OrderBy(r => r.GetHashCode()))
        {
            foreach (var component in role.GetEqualityComponents())
                yield return component;
        }
    }
}