using FluentAssertions;
using KeeperData.Api.Worker.Jobs;
using KeeperData.Api.Worker.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Quartz;
using Xunit;

namespace KeeperData.Api.Worker.Tests.Unit.Jobs;

public class SamBulkScanJobTests
{
    private readonly Mock<ISamBulkScanTask> _taskMock;
    private readonly Mock<ILogger<SamBulkScanJob>> _loggerMock;
    private readonly Mock<IJobExecutionContext> _contextMock;
    private readonly SamBulkScanJob _sut;

    public SamBulkScanJobTests()
    {
        _taskMock = new Mock<ISamBulkScanTask>();
        _loggerMock = new Mock<ILogger<SamBulkScanJob>>();
        _contextMock = new Mock<IJobExecutionContext>();
        _sut = new SamBulkScanJob(_taskMock.Object, _loggerMock.Object);
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
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Execute_WhenTaskFails_ShouldLogAndRethrow()
    {
        _taskMock.Setup(x => x.RunAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("Fail"));

        await _sut.Invoking(s => s.Execute(_contextMock.Object)).Should().ThrowAsync<Exception>();
    }
}