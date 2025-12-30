using FluentAssertions;
using KeeperData.Api.Worker.Jobs;
using KeeperData.Api.Worker.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Quartz;
using Xunit;

namespace KeeperData.Api.Worker.Tests.Unit.Jobs;

public class SamDailyScanJobTests
{
    private readonly Mock<ISamDailyScanTask> _taskMock;
    private readonly Mock<ILogger<SamDailyScanJob>> _loggerMock;
    private readonly Mock<IJobExecutionContext> _contextMock;
    private readonly SamDailyScanJob _sut;

    public SamDailyScanJobTests()
    {
        _taskMock = new Mock<ISamDailyScanTask>();
        _loggerMock = new Mock<ILogger<SamDailyScanJob>>();
        _contextMock = new Mock<IJobExecutionContext>();
        _sut = new SamDailyScanJob(_taskMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Execute_ShouldRunTaskAndLogSuccess()
    {
        var cancellationToken = new CancellationToken();
        _contextMock.Setup(c => c.CancellationToken).Returns(cancellationToken);

        await _sut.Execute(_contextMock.Object);

        _taskMock.Verify(x => x.RunAsync(cancellationToken), Times.Once);
        _loggerMock.Verify(x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("started")),
            null, It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Execute_WhenTaskFails_ShouldLogAndRethrow()
    {
        _taskMock.Setup(x => x.RunAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("Fail"));

        await _sut.Invoking(s => s.Execute(_contextMock.Object)).Should().ThrowAsync<Exception>();

        _loggerMock.Verify(x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("failed")),
            It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}