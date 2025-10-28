using KeeperData.Core.Domain.BuildingBlocks;
using KeeperData.Core.Domain.BuildingBlocks.Aggregates;
using KeeperData.Core.Domain.Parties.DomainEvents;

namespace KeeperData.Core.Domain.Parties;

public class Party : IAggregateRoot
{
    public string Id { get; private set; }
    public int LastUpdatedBatchId { get; set; }
    public DateTime LastUpdatedDate { get; private set; }
    public string? Title { get; private set; }
    public string? FirstName { get; private set; }
    public string? LastName { get; private set; }
    public string? Name { get; private set; }
    public string? CustomerNumber { get; private set; }
    public string? PartyType { get; private set; }
    public string? State { get; private set; }
    public bool Deleted { get; private set; }

    public Party(
        string id,
        int batchId,
        DateTime lastUpdatedDate,
        string? title,
        string?
        firstName,
        string? lastName,
        string? name,
        string? customerNumber,
        string? partyType,
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
        Deleted = deleted;

        _domainEvents.Add(new PartyCreatedDomainEvent(Id));
    }

    public static Party Create(
        int batchId,
        DateTime lastUpdatedDate,
        string? title,
        string? firstName,
        string? lastName,
        string? name,
        string? customerNumber,
        string? partyType,
        bool deleted)
    {
        return new Party(
            Guid.NewGuid().ToString(),
            batchId,
            lastUpdatedDate,
            title,
            firstName,
            lastName,
            name,
            customerNumber,
            partyType,
            deleted);
    }

    public void Delete(int batchId)
    {
        if (Deleted) return;

        Deleted = true;
        State = "Inactive";
        LastUpdatedBatchId = batchId;
        LastUpdatedDate = DateTime.UtcNow;
    }

    public void UpdateLastUpdatedDate(DateTime lastUpdatedDate)
    {
        LastUpdatedDate = lastUpdatedDate;
    }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents;
    public void ClearDomainEvents() => _domainEvents.Clear();
    private readonly List<IDomainEvent> _domainEvents = [];
}