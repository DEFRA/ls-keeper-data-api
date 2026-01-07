using FluentAssertions;
using KeeperData.Application.Orchestration.Updates;
using Microsoft.Extensions.Logging;
using Moq;

namespace KeeperData.Application.Tests.Unit.Orchestration;

public class UpdateBaseStepTests
{
    public class TestUpdateContext { }

    public class TestUpdateStep(ILogger logger) : UpdateStepBase<TestUpdateContext>(logger)
    {
        public bool Executed { get; private set; }
        protected override Task ExecuteCoreAsync(TestUpdateContext context, CancellationToken cancellationToken)
        {
            Executed = true;
            return Task.CompletedTask;
        }
    }

    public class ThrowingUpdateStep(ILogger logger) : UpdateStepBase<TestUpdateContext>(logger)
    {
        protected override Task ExecuteCoreAsync(TestUpdateContext context, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Fail");
        }
    }

    [Fact]
    public async Task ExecuteAsync_CallsCore_AndLogs()
    {
        var loggerMock = new Mock<ILogger>();
        var step = new TestUpdateStep(loggerMock.Object);

        await step.ExecuteAsync(new TestUpdateContext(), CancellationToken.None);

        step.Executed.Should().BeTrue();

        loggerMock.Verify(x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Starting update step")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);

        loggerMock.Verify(x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Completed update step")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_CatchesException_LogsError_AndRethrows()
    {
        var loggerMock = new Mock<ILogger>();
        var step = new ThrowingUpdateStep(loggerMock.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() => step.ExecuteAsync(new TestUpdateContext(), CancellationToken.None));

        loggerMock.Verify(x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error in update step")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }
}