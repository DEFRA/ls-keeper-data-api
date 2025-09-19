using KeeperData.Core.Repositories;

namespace KeeperData.Core.Domain.BuildingBlocks.Aggregates;

public interface IAggregateRoot : IEntity
{
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }
    void ClearDomainEvents();
}