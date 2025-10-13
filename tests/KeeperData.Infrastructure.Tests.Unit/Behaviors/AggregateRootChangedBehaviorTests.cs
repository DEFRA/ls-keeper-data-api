using FluentAssertions;
using KeeperData.Core.Domain.BuildingBlocks;
using KeeperData.Core.Domain.BuildingBlocks.Aggregates;
using KeeperData.Infrastructure.Behaviors;
using MediatR;
using Moq;

namespace KeeperData.Infrastructure.Tests.Unit.Behaviors;

public class AggregateRootChangedBehaviorTests
{
    public record TrackedResultRequest : IRequest<ITrackedResult>;
    public record AggregateRootRequest : IRequest<IAggregateRoot>;
    public record EnumerableRequest : IRequest<IEnumerable<IAggregateRoot>>;
    public record NestedResultRequest : IRequest<NestedResult>;
    public record NullableRequest : IRequest<object?>;

    public class TestAggregate : IAggregateRoot
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public int LastUpdatedBatchId { get; set; } = 1;
        public IReadOnlyCollection<IDomainEvent> DomainEvents => [];
        public void ClearDomainEvents() { }
    }

    public class TrackedResult(IEnumerable<IAggregateRoot> aggregates) : ITrackedResult
    {
        public IReadOnlyCollection<IAggregateRoot> Aggregates { get; } = [.. aggregates];
    }

    public class NestedResult
    {
        public IAggregateRoot? Aggregate { get; set; }
        public List<IAggregateRoot>? AggregateList { get; set; }
    }

    [Fact]
    public async Task Handle_ShouldTrackAggregates_FromTrackedResult()
    {
        var aggregate = new TestAggregate();
        var trackerMock = new Mock<IAggregateTracker>();
        var behavior = new AggregateRootChangedBehavior<TrackedResultRequest, ITrackedResult>(trackerMock.Object);

        var trackedResult = new TrackedResult([aggregate]);
        Task<ITrackedResult> next(CancellationToken _) => Task.FromResult<ITrackedResult>(trackedResult);

        var result = await behavior.Handle(new TrackedResultRequest(), next, CancellationToken.None);

        result.Should().BeSameAs(trackedResult);
        trackerMock.Verify(x => x.Track(aggregate), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldTrackAggregate_WhenReturnedDirectly()
    {
        var aggregate = new TestAggregate();
        var trackerMock = new Mock<IAggregateTracker>();
        var behavior = new AggregateRootChangedBehavior<AggregateRootRequest, IAggregateRoot>(trackerMock.Object);

        Task<IAggregateRoot> next(CancellationToken _) => Task.FromResult<IAggregateRoot>(aggregate);

        var result = await behavior.Handle(new AggregateRootRequest(), next, CancellationToken.None);

        result.Should().BeSameAs(aggregate);
        trackerMock.Verify(x => x.Track(aggregate), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldTrackAggregates_FromEnumerable()
    {
        var aggregate1 = new TestAggregate();
        var aggregate2 = new TestAggregate();
        var trackerMock = new Mock<IAggregateTracker>();
        var behavior = new AggregateRootChangedBehavior<EnumerableRequest, IEnumerable<IAggregateRoot>>(trackerMock.Object);

        IEnumerable<IAggregateRoot> resultSet = [aggregate1, aggregate2];
        Task<IEnumerable<IAggregateRoot>> next(CancellationToken _) => Task.FromResult(resultSet);

        var result = await behavior.Handle(new EnumerableRequest(), next, CancellationToken.None);

        result.Should().BeSameAs(resultSet);
        trackerMock.Verify(x => x.Track(aggregate1), Times.Once);
        trackerMock.Verify(x => x.Track(aggregate2), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldTrackAggregates_FromNestedProperties()
    {
        var aggregate1 = new TestAggregate();
        var aggregate2 = new TestAggregate();
        var nested = new NestedResult
        {
            Aggregate = aggregate1,
            AggregateList = [aggregate2]
        };

        var trackerMock = new Mock<IAggregateTracker>();
        var behavior = new AggregateRootChangedBehavior<NestedResultRequest, NestedResult>(trackerMock.Object);

        Task<NestedResult> next(CancellationToken _) => Task.FromResult(nested);

        var result = await behavior.Handle(new NestedResultRequest(), next, CancellationToken.None);

        result.Should().BeSameAs(nested);
        trackerMock.Verify(x => x.Track(aggregate1), Times.Once);
        trackerMock.Verify(x => x.Track(aggregate2), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldNotFail_WhenResultIsNull()
    {
        var trackerMock = new Mock<IAggregateTracker>();
        var behavior = new AggregateRootChangedBehavior<NullableRequest, object?>(trackerMock.Object);

        static Task<object?> next(CancellationToken _) => Task.FromResult<object?>(null);

        var result = await behavior.Handle(new NullableRequest(), next, CancellationToken.None);

        result.Should().BeNull();
        trackerMock.Verify(x => x.Track(It.IsAny<IAggregateRoot>()), Times.Never);
    }
}