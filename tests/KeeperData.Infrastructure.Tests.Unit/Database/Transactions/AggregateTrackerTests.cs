using FluentAssertions;
using KeeperData.Core.Domain.BuildingBlocks;
using KeeperData.Core.Domain.BuildingBlocks.Aggregates;
using KeeperData.Infrastructure.Database.Transactions;

namespace KeeperData.Infrastructure.Tests.Unit.Database.Transactions;

public class AggregateTrackerTests
{
    private class TestAggregate : IAggregateRoot
    {
        public IReadOnlyCollection<IDomainEvent> DomainEvents => [];

        public string Id { get; set; } = Guid.NewGuid().ToString();

        public void ClearDomainEvents() { }
    }

    [Fact]
    public void Track_ShouldAddAggregate()
    {
        var tracker = new AggregateTracker();
        var aggregate = new TestAggregate();

        tracker.Track(aggregate);

        tracker.GetTrackedAggregates().Should().ContainSingle().Which.Should().Be(aggregate);
    }

    [Fact]
    public void Track_ShouldIgnoreNull()
    {
        var tracker = new AggregateTracker();
        tracker.Track(null!);

        tracker.GetTrackedAggregates().Should().BeEmpty();
    }

    [Fact]
    public void Clear_ShouldRemoveAllTrackedAggregates()
    {
        var tracker = new AggregateTracker();
        tracker.Track(new TestAggregate());
        tracker.Track(new TestAggregate());

        tracker.Clear();

        tracker.GetTrackedAggregates().Should().BeEmpty();
    }
}