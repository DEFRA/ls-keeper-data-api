using KeeperData.Core.Domain.BuildingBlocks;

namespace KeeperData.Core.Domain.Parties.DomainEvents;

public class PartyUpdatedDomainEvent(string id) : IDomainEvent
{
    public string Id { get; } = id;
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}