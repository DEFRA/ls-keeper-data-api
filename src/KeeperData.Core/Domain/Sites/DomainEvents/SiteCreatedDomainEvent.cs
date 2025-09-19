using KeeperData.Core.Domain.BuildingBlocks;

namespace KeeperData.Core.Domain.Sites.DomainEvents;

/// <summary>
/// Example implementation only. To remove in future stories.
/// </summary>
public class SiteCreatedDomainEvent(string id) : IDomainEvent
{
    public string Id { get; } = id;
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}