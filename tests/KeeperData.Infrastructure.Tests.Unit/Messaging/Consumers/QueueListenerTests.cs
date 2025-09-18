using KeeperData.Core.Messaging.Consumers;
using KeeperData.Infrastructure.Messaging.Consumers;
using KeeperData.Infrastructure.Tests.Unit.Helpers;
using Microsoft.Extensions.Logging;
using Moq;

namespace KeeperData.Infrastructure.Tests.Unit.Messaging.Consumers;

public class QueueListenerTests
{
    private readonly Mock<IQueuePoller> _queuePollerMock = new();
    private readonly Mock<ILogger<QueueListener>> _loggerMock = new();

    private readonly QueueListener _sut;

    public QueueListenerTests()
    {
        _sut = new QueueListener(_queuePollerMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task StartAsync_ShouldLogStart_ThenStartThePoller()
    {
        await _sut.StartAsync(new CancellationToken());

        // Assert
        _loggerMock.VerifyLog(LogLevel.Information, "QueueListener start requested.", Times.Exactly(1));
        _queuePollerMock.Verify(x => x.StartAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task StopAsync_ShouldLogStop_ThenReturnCompletedTask()
    {
        await _sut.StopAsync(new CancellationToken());

        // Assert
        _loggerMock.VerifyLog(LogLevel.Information, "QueueListener stop requested.", Times.Exactly(1));
        _queuePollerMock.Verify(x => x.StopAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}