using KeeperData.Core.Domain.BuildingBlocks;
using KeeperData.Core.Domain.BuildingBlocks.Aggregates;
using KeeperData.Core.Domain.Parties.DomainEvents;
using KeeperData.Core.Domain.Shared;

namespace KeeperData.Core.Domain.Parties;

public class Party : IAggregateRoot
{
    public string Id { get; private set; }
    public DateTime CreatedDate { get; private set; }
    public DateTime LastUpdatedDate { get; private set; }
    public string? Title { get; private set; }
    public string? FirstName { get; private set; }
    public string? LastName { get; private set; }
    public string? Name { get; private set; }
    public string? CustomerNumber { get; private set; }
    public string? PartyType { get; private set; }
    public string? State { get; private set; }
    public bool Deleted { get; private set; }
    public bool IsInsert { get; private set; }

    private readonly List<Communication> _communications = [];
    public IReadOnlyCollection<Communication> Communications => _communications.AsReadOnly();

    private Address? _address;
    public Address? Address => _address;

    private readonly List<PartyRole> _roles = [];
    public IReadOnlyCollection<PartyRole> Roles => _roles.AsReadOnly();

    private readonly List<IDomainEvent> _domainEvents = [];
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    public void ClearDomainEvents() => _domainEvents.Clear();

    public Party(
        string id,
        DateTime createdDate,
        DateTime lastUpdatedDate,
        string? title,
        string? firstName,
        string? lastName,
        string? name,
        string? customerNumber,
        string? partyType,
        string? state,
        bool deleted,
        Address? address = null)
    {
        Id = id;
        CreatedDate = createdDate;
        LastUpdatedDate = lastUpdatedDate;
        Title = title;
        FirstName = firstName;
        LastName = lastName;
        Name = name;
        CustomerNumber = customerNumber;
        PartyType = partyType;
        State = state;
        Deleted = deleted;
        _address = address;
    }

    public static Party Create(
        DateTime createdDate,
        DateTime lastUpdatedDate,
        string? title,
        string? firstName,
        string? lastName,
        string? name,
        string? customerNumber,
        string? partyType,
        string? state,
        bool deleted,
        Address? address = null)
    {
        var party = new Party(
            Guid.NewGuid().ToString(),
            createdDate,
            lastUpdatedDate,
            title,
            firstName,
            lastName,
            name,
            customerNumber,
            partyType,
            state,
            deleted,
            address)
        {
            IsInsert = true
        };

        party._domainEvents.Add(new PartyCreatedDomainEvent(party.Id));
        return party;
    }

    public void Update(
        DateTime lastUpdatedDate,
        string? title,
        string? firstName,
        string? lastName,
        string? name,
        string? customerNumber,
        string? partyType,
        string? state,
        bool deleted)
    {
        var changed = false;

        changed |= Change(Title, title, v => Title = v, lastUpdatedDate);
        changed |= Change(FirstName, firstName, v => FirstName = v, lastUpdatedDate);
        changed |= Change(LastName, lastName, v => LastName = v, lastUpdatedDate);
        changed |= Change(Name, name, v => Name = v, lastUpdatedDate);
        changed |= Change(CustomerNumber, customerNumber, v => CustomerNumber = v, lastUpdatedDate);
        changed |= Change(PartyType, partyType, v => PartyType = v, lastUpdatedDate);
        changed |= Change(State, state, v => State = v, lastUpdatedDate);
        changed |= Change(Deleted, deleted, v => Deleted = v, lastUpdatedDate);

        if (changed)
        {
            _domainEvents.Add(new PartyUpdatedDomainEvent(Id));
        }
    }

    public void Delete()
    {
        if (Deleted) return;

        Deleted = true;
        State = "Inactive";
        LastUpdatedDate = DateTime.UtcNow;
    }

    public void SetAddress(Address address)
    {
        _address = address;
    }

    public void SetAddress(
        DateTime lastUpdatedDate,
        Address address)
    {
        if (_address is null)
        {
            _address = Address.Create(
                address.Uprn,
                address.AddressLine1,
                address.AddressLine2,
                address.PostTown,
                address.County,
                address.PostCode,
                address.Country);
            UpdateLastUpdatedDate(lastUpdatedDate);
            return;
        }

        if (_address.ApplyChanges(
            lastUpdatedDate,
            address.Uprn,
            address.AddressLine1,
            address.AddressLine2,
            address.PostTown,
            address.County,
            address.PostCode,
            address.Country))
        {
            UpdateLastUpdatedDate(lastUpdatedDate);
        }
    }

    public void SetCommunications(Communication communication)
    {
        _communications.Clear();
        _communications.Add(communication);
    }

    public void AddOrUpdatePrimaryCommunication(
        DateTime lastUpdatedDate,
        Communication communication)
    {
        var existing = _communications.FirstOrDefault();

        if (existing is null)
        {
            var newCommunication = Communication.Create(
                communication.Email,
                communication.Mobile,
                communication.Landline,
                communication.PrimaryContactFlag);
            _communications.Add(newCommunication);
            UpdateLastUpdatedDate(DateTime.UtcNow);
            return;
        }

        if (existing.ApplyChanges(
            lastUpdatedDate,
            communication.Email,
            communication.Mobile,
            communication.Landline,
            communication.PrimaryContactFlag))
        {
            UpdateLastUpdatedDate(lastUpdatedDate);
        }
    }

    public void SetRoles(IEnumerable<PartyRole> roles)
    {
        _roles.Clear();
        _roles.AddRange(roles);
    }

    public void AddOrUpdateRole(DateTime lastUpdatedDate, PartyRole incoming)
    {
        var existing = _roles.FirstOrDefault(r =>
            r.Role.Id == incoming.Role.Id &&
            r.Site?.Id == incoming.Site?.Id);

        if (existing is null)
        {
            var newRole = PartyRole.Create(incoming.Site, incoming.Role, incoming.SpeciesManagedByRole);
            _roles.Add(newRole);
            UpdateLastUpdatedDate(lastUpdatedDate);
            return;
        }

        if (existing.ApplyChanges(incoming.Site, incoming.Role, incoming.SpeciesManagedByRole, lastUpdatedDate))
        {
            UpdateLastUpdatedDate(lastUpdatedDate);
        }
    }

    public void DeleteRole(string roleId, string siteId)
    {
        var existing = _roles.FirstOrDefault(r =>
            r.Role.Id == roleId &&
            r.Site?.Id == siteId);

        if (existing is not null)
        {
            _roles.Remove(existing);
            UpdateLastUpdatedDate(DateTime.UtcNow);
        }
    }

    private bool Change<T>(T currentValue, T newValue, Action<T> setter, DateTime lastUpdatedAt)
    {
        if (EqualityComparer<T>.Default.Equals(currentValue, newValue)) return false;
        setter(newValue);
        LastUpdatedDate = lastUpdatedAt;
        return true;
    }

    private void UpdateLastUpdatedDate(DateTime lastUpdatedDate)
    {
        LastUpdatedDate = lastUpdatedDate;
    }
}