using KeeperData.Core.Domain.BuildingBlocks;

namespace KeeperData.Core.Domain.Sites.DomainEvents;

public class SiteCreatedDomainEvent(string id) : IDomainEvent
{
    public string Id { get; } = id;
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}