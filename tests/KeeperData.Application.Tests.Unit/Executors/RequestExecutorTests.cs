using FluentAssertions;
using KeeperData.Application.Commands;
using KeeperData.Application.Queries;
using KeeperData.Core.Domain.BuildingBlocks.Aggregates;
using MediatR;
using Moq;

namespace KeeperData.Application.Tests.Unit.Executors;

public class RequestExecutorTests
{
    [Fact]
    public async Task ExecuteCommand_ShouldSendCommandViaMediator()
    {
        var command = new TestCommand("create");
        var expectedResult = Guid.NewGuid().ToString();

        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(m => m.Send(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        var executor = new RequestExecutor(mediatorMock.Object);

        var result = await executor.ExecuteCommand(command);

        result.Should().Be(expectedResult);
        mediatorMock.Verify(m => m.Send(command, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteQuery_ShouldSendQueryViaMediator()
    {
        var id = Guid.NewGuid().ToString();
        var query = new TestQuery(id);
        var expectedResult = new TestResult(id, "Test Entity");

        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(m => m.Send(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        var executor = new RequestExecutor(mediatorMock.Object);

        var result = await executor.ExecuteQuery(query);

        result.Should().BeEquivalentTo(expectedResult);
        mediatorMock.Verify(m => m.Send(query, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteCommand_WithTrackedResult_ShouldUnwrapAndReturnResult()
    {
        var expectedResult = new TestResult("123", "Tracked");
        var trackedResult = new TrackedResult<TestResult>(expectedResult);

        var command = new TrackedTestCommand("create");

        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(m => m.Send(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(trackedResult);

        var executor = new RequestExecutor(mediatorMock.Object);

        var result = await executor.ExecuteCommand(command);

        result.Should().BeEquivalentTo(expectedResult);
        mediatorMock.Verify(m => m.Send(command, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteTrackedCommand_ShouldReturnFullTrackedResult()
    {
        var expectedResult = new TestResult("456", "Full");
        var trackedResult = new TrackedResult<TestResult>(expectedResult);

        var command = new TrackedTestCommand("update");

        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(m => m.Send(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(trackedResult);

        var executor = new RequestExecutor(mediatorMock.Object);

        var result = await executor.ExecuteTrackedCommand<TestResult>(command);

        result.Should().BeSameAs(trackedResult);
        result.Result.Should().BeEquivalentTo(expectedResult);
        mediatorMock.Verify(m => m.Send(command, It.IsAny<CancellationToken>()), Times.Once);
    }

    public record TestCommand(string Name) : ICommand<string>;
    public record TestQuery(string Id) : IQuery<TestResult>;
    public record TrackedTestCommand(string Name) : ICommand<TrackedResult<TestResult>>;
    public record TestResult(string Id, string Value);
}