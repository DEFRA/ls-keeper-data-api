using FluentAssertions;
using KeeperData.Application.Commands;
using KeeperData.Core.Domain.BuildingBlocks;
using KeeperData.Core.Domain.BuildingBlocks.Aggregates;
using KeeperData.Infrastructure.Behaviors;
using MediatR;
using Moq;

namespace KeeperData.Infrastructure.Tests.Unit.Behaviors;

public class DomainEventDispatchingBehaviorTests
{
    public class TestRequest : ICommand<string> { }
    public class DomainEventDispatchingTestDomainEvent(string id) : IDomainEvent
    {
        public string Id { get; } = id;
        public DateTime OccurredOn { get; } = DateTime.UtcNow;
    }

    [Fact]
    public async Task Handle_ShouldPublishDomainEvents_AndClearThem_AndReturnResponse()
    {
        var domainEvent = new DomainEventDispatchingTestDomainEvent(Guid.NewGuid().ToString());

        var aggregateMock = new Mock<IAggregateRoot>();
        aggregateMock.SetupGet(x => x.DomainEvents).Returns([domainEvent]);
        aggregateMock.Setup(x => x.ClearDomainEvents());

        var trackerMock = new Mock<IAggregateTracker>();
        trackerMock.Setup(x => x.GetTrackedAggregates()).Returns([aggregateMock.Object]);
        trackerMock.Setup(x => x.Clear());

        var mediatorMock = new Mock<IMediator>();
        mediatorMock.Setup(x => x.Publish(domainEvent, It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);

        var behavior = new DomainEventDispatchingBehavior<TestRequest, string>(trackerMock.Object, mediatorMock.Object);

        static Task<string> next(CancellationToken token = default) => Task.FromResult("SingleEvent");

        var result = await behavior.Handle(new TestRequest(), next, CancellationToken.None);

        result.Should().Be("SingleEvent");

        mediatorMock.Verify(x => x.Publish(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()), Times.Once);
        aggregateMock.Verify(x => x.ClearDomainEvents(), Times.Once);
        trackerMock.Verify(x => x.Clear(), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldNotFail_WhenNoAggregatesTracked()
    {
        var trackerMock = new Mock<IAggregateTracker>();
        trackerMock.Setup(x => x.GetTrackedAggregates()).Returns([]);
        trackerMock.Setup(x => x.Clear());

        var mediatorMock = new Mock<IMediator>();

        var behavior = new DomainEventDispatchingBehavior<TestRequest, string>(trackerMock.Object, mediatorMock.Object);

        static Task<string> next(CancellationToken token = default) => Task.FromResult("NoAggregates");

        var result = await behavior.Handle(new TestRequest(), next, CancellationToken.None);

        result.Should().Be("NoAggregates");
        mediatorMock.Verify(x => x.Publish(It.IsAny<INotification>(), It.IsAny<CancellationToken>()), Times.Never);
        trackerMock.Verify(x => x.Clear(), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldPublishMultipleEventsAcrossMultipleAggregates()
    {
        var event1 = new DomainEventDispatchingTestDomainEvent(Guid.NewGuid().ToString());
        var event2 = new DomainEventDispatchingTestDomainEvent(Guid.NewGuid().ToString());

        var aggregate1 = new Mock<IAggregateRoot>();
        aggregate1.SetupGet(x => x.DomainEvents).Returns([event1]);
        aggregate1.Setup(x => x.ClearDomainEvents());

        var aggregate2 = new Mock<IAggregateRoot>();
        aggregate2.SetupGet(x => x.DomainEvents).Returns([event2]);
        aggregate2.Setup(x => x.ClearDomainEvents());

        var trackerMock = new Mock<IAggregateTracker>();
        trackerMock.Setup(x => x.GetTrackedAggregates()).Returns([aggregate1.Object, aggregate2.Object]);
        trackerMock.Setup(x => x.Clear());

        var mediatorMock = new Mock<IMediator>();
        mediatorMock.Setup(x => x.Publish(It.IsAny<INotification>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);

        var behavior = new DomainEventDispatchingBehavior<TestRequest, string>(trackerMock.Object, mediatorMock.Object);
        var result = await behavior.Handle(new TestRequest(), (token) => Task.FromResult("MultipleEvents"), CancellationToken.None);

        result.Should().Be("MultipleEvents");

        mediatorMock.Verify(x => x.Publish(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        aggregate1.Verify(x => x.ClearDomainEvents(), Times.Once);
        aggregate2.Verify(x => x.ClearDomainEvents(), Times.Once);
        trackerMock.Verify(x => x.Clear(), Times.Once);
    }
}