using KeeperData.Core.Domain.BuildingBlocks;
using KeeperData.Core.Domain.BuildingBlocks.Aggregates;
using KeeperData.Core.Domain.Parties.DomainEvents;

namespace KeeperData.Core.Domain.Parties;

public class Party : IAggregateRoot
{
    public string Id { get; private set; }
    public DateTime LastUpdatedDate { get; private set; }
    public string? FirstName { get; private set; }
    public string? LastName { get; private set; }
    public string? Name { get; private set; }

    public Party(
        string id,
        DateTime lastUpdatedDate,
        string? firstName,
        string? lastName,
        string? name)
    {
        Id = id;
        LastUpdatedDate = lastUpdatedDate;
        FirstName = firstName;
        LastName = lastName;
        Name = name;
        _domainEvents.Add(new PartyCreatedDomainEvent(Id));
    }

    public static Party Create(
        string? firstName,
        string? lastName,
        string? name)
    {
        return new Party(
            Guid.NewGuid().ToString(),
            DateTime.UtcNow,
            firstName,
            lastName,
            name);
    }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents;
    public void ClearDomainEvents() => _domainEvents.Clear();
    private readonly List<IDomainEvent> _domainEvents = [];
}