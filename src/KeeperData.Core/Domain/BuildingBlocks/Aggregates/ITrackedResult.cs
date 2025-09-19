namespace KeeperData.Core.Domain.BuildingBlocks.Aggregates;

public interface ITrackedResult
{
    IReadOnlyCollection<IAggregateRoot> Aggregates { get; }
}