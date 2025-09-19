using MediatR;

namespace KeeperData.Core.Domain.BuildingBlocks;

public interface IDomainEvent : INotification
{
    DateTime OccurredOn { get; }
}