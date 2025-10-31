using KeeperData.Core.Domain.BuildingBlocks;
using KeeperData.Core.Domain.BuildingBlocks.Aggregates;
using KeeperData.Core.Domain.Parties.DomainEvents;

namespace KeeperData.Core.Domain.Parties;

public class Party : IAggregateRoot
{
    public string Id { get; private set; }
    public int LastUpdatedBatchId { get; private set; }
    public DateTime LastUpdatedDate { get; private set; }
    public string? Title { get; private set; }
    public string? FirstName { get; private set; }
    public string? LastName { get; private set; }
    public string? Name { get; private set; }
    public string? CustomerNumber { get; private set; }
    public string? PartyType { get; private set; }
    public string? State { get; private set; }
    public bool Deleted { get; private set; }

    private readonly List<IDomainEvent> _domainEvents = [];
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    public void ClearDomainEvents() => _domainEvents.Clear();

    public Party(
        string id,
        int batchId,
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
        Id = id;
        LastUpdatedBatchId = batchId;
        LastUpdatedDate = lastUpdatedDate;
        Title = title;
        FirstName = firstName;
        LastName = lastName;
        Name = name;
        CustomerNumber = customerNumber;
        PartyType = partyType;
        State = state;
        Deleted = deleted;
    }

    public static Party Create(
        int batchId,
        string? title,
        string? firstName,
        string? lastName,
        string? name,
        string? customerNumber,
        string? partyType,
        string? state,
        bool deleted)
    {
        var party = new Party(
            Guid.NewGuid().ToString(),
            batchId,
            DateTime.UtcNow,
            title,
            firstName,
            lastName,
            name,
            customerNumber,
            partyType,
            state,
            deleted);

        party._domainEvents.Add(new PartyCreatedDomainEvent(party.Id));
        return party;
    }

    public void Update(
        DateTime lastUpdatedDate,
        int batchId,
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

        changed |= Change(LastUpdatedBatchId, batchId, v => LastUpdatedBatchId = v, lastUpdatedDate);
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

    public void Delete(int batchId)
    {
        if (Deleted) return;

        Deleted = true;
        State = "Inactive";
        LastUpdatedBatchId = batchId;
        LastUpdatedDate = DateTime.UtcNow;
    }

    private bool Change<T>(T currentValue, T newValue, Action<T> setter, DateTime lastUpdatedAt)
    {
        if (EqualityComparer<T>.Default.Equals(currentValue, newValue)) return false;
        setter(newValue);
        LastUpdatedDate = lastUpdatedAt;
        return true;
    }
}