using FluentAssertions;
using KeeperData.Api.Worker.Jobs;
using KeeperData.Api.Worker.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Quartz;
using Xunit;

namespace KeeperData.Api.Worker.Tests.Unit.Jobs;

public class CtsBulkScanJobTests
{
    private readonly Mock<ICtsBulkScanTask> _taskMock;
    private readonly Mock<ILogger<CtsBulkScanJob>> _loggerMock;
    private readonly Mock<IJobExecutionContext> _contextMock;
    private readonly CtsBulkScanJob _sut;

    public CtsBulkScanJobTests()
    {
        _taskMock = new Mock<ICtsBulkScanTask>();
        _loggerMock = new Mock<ILogger<CtsBulkScanJob>>();
        _contextMock = new Mock<IJobExecutionContext>();
        _sut = new CtsBulkScanJob(_taskMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Execute_ShouldRunTaskAndLogSuccess()
    {
        // Arrange
        var cancellationToken = new CancellationToken();
        _contextMock.Setup(c => c.CancellationToken).Returns(cancellationToken);

        // Act
        await _sut.Execute(_contextMock.Object);

        // Assert
        _taskMock.Verify(x => x.RunAsync(cancellationToken), Times.Once);

        _loggerMock.Verify(x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("started")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);

        _loggerMock.Verify(x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("completed")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public async Task Execute_WhenTaskFails_ShouldLogAndRethrow()
    {
        // Arrange
        var expectedException = new Exception("Task failure");
        _taskMock.Setup(x => x.RunAsync(It.IsAny<CancellationToken>())).ThrowsAsync(expectedException);

        // Act & Assert
        await _sut.Invoking(s => s.Execute(_contextMock.Object))
            .Should().ThrowAsync<Exception>().WithMessage("Task failure");
    }
}