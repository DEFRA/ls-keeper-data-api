using FluentAssertions;
using KeeperData.Application.Orchestration.ChangeScanning;
using KeeperData.Application.Orchestration.Imports;
using KeeperData.Core.Orchestration;
using KeeperData.Core.Telemetry;
using Microsoft.Extensions.Logging;
using Moq;

namespace KeeperData.Application.Tests.Unit.Orchestration;

public class BaseClassesTests
{
    public class TestContext { }

    public class TestScanStep(ILogger logger) : ScanStepBase<TestContext>(logger)
    {
        public bool ExecuteCoreCalled { get; private set; }
        protected override Task ExecuteCoreAsync(TestContext context, CancellationToken cancellationToken)
        {
            ExecuteCoreCalled = true;
            return Task.CompletedTask;
        }
    }

    public class ThrowingScanStep(ILogger logger) : ScanStepBase<TestContext>(logger)
    {
        protected override Task ExecuteCoreAsync(TestContext context, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Step failed");
        }
    }

    [Fact]
    public async Task ScanStepBase_ExecuteAsync_LogsAndCallsCore()
    {
        var loggerMock = new Mock<ILogger>();
        var step = new TestScanStep(loggerMock.Object);
        var context = new TestContext();

        await step.ExecuteAsync(context, CancellationToken.None);

        step.ExecuteCoreCalled.Should().BeTrue();

        loggerMock.Verify(x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Starting scan step")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);

        loggerMock.Verify(x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Completed scan step")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public async Task ScanStepBase_ExecuteAsync_WhenException_LogsErrorAndRethrows()
    {
        var loggerMock = new Mock<ILogger>();
        var step = new ThrowingScanStep(loggerMock.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() => step.ExecuteAsync(new TestContext(), CancellationToken.None));

        loggerMock.Verify(x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error in scan step")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    public class TestImportStep(ILogger logger) : ImportStepBase<TestContext>(logger)
    {
        protected override Task ExecuteCoreAsync(TestContext context, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    [Fact]
    public async Task ImportStepBase_ExecuteAsync_Logs()
    {
        var loggerMock = new Mock<ILogger>();
        var step = new TestImportStep(loggerMock.Object);

        await step.ExecuteAsync(new TestContext(), CancellationToken.None);

        loggerMock.Verify(x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.AtLeast(2));
    }

    public class TestScanOrchestrator(IEnumerable<IScanStep<TestContext>> steps, IApplicationMetrics metrics) : ScanOrchestrator<TestContext>(steps, metrics) { }

    [Fact]
    public async Task ScanOrchestrator_ExecuteAsync_RunsAllSteps()
    {
        var step1 = new Mock<IScanStep<TestContext>>();
        var step2 = new Mock<IScanStep<TestContext>>();
        var orchestrator = new TestScanOrchestrator([step1.Object, step2.Object], Mock.Of<IApplicationMetrics>());
        var context = new TestContext();

        await orchestrator.ExecuteAsync(context, CancellationToken.None);

        step1.Verify(x => x.ExecuteAsync(context, CancellationToken.None), Times.Once);
        step2.Verify(x => x.ExecuteAsync(context, CancellationToken.None), Times.Once);
    }
}