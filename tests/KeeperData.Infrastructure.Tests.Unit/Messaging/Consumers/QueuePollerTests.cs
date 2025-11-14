using Amazon.SQS;
using Amazon.SQS.Model;
using FluentAssertions;
using KeeperData.Core.Messaging.Contracts;
using KeeperData.Core.Messaging.MessageHandlers;
using KeeperData.Core.Messaging.Observers;
using KeeperData.Core.Messaging.Serializers;
using KeeperData.Infrastructure.Messaging.Configuration;
using KeeperData.Infrastructure.Messaging.Consumers;
using KeeperData.Infrastructure.Messaging.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace KeeperData.Infrastructure.Tests.Unit.Messaging.Consumers;

public class QueuePollerTests
{
    private readonly Mock<IServiceScopeFactory> _scopeFactoryMock = new();
    private readonly Mock<IAmazonSQS> _sqsMock = new();
    private readonly Mock<IMessageHandlerManager> _handlerManagerMock = new();
    private readonly Mock<IMessageSerializer<SnsEnvelope>> _serializerMock = new();
    private readonly Mock<ILogger<QueuePoller>> _loggerMock = new();
    private readonly Mock<IServiceScope> _scopeMock = new();
    private readonly Mock<IServiceProvider> _providerMock = new();
    private readonly Mock<IDeadLetterQueueService> _deadLetterQueueServiceMock = new();

    private readonly IntakeEventQueueOptions _options = new()
    {
        QueueUrl = "http://localhost:4566/000000000000/test-queue",
        WaitTimeSeconds = 1,
        MaxNumberOfMessages = 1,
        Disabled = false
    };

    private QueuePoller CreateSut()
    {
        _scopeFactoryMock.Setup(x => x.CreateScope()).Returns(_scopeMock.Object);
        _scopeMock.Setup(x => x.ServiceProvider).Returns(_providerMock.Object);
        _providerMock.Setup(x => x.GetService(typeof(IQueuePollerObserver<MessageType>)))
            .Returns(Mock.Of<IQueuePollerObserver<MessageType>>());

        return new QueuePoller(
            _scopeFactoryMock.Object,
            _sqsMock.Object,
            _handlerManagerMock.Object,
            _serializerMock.Object,
            _deadLetterQueueServiceMock.Object,
            Options.Create(_options),
            _loggerMock.Object);
    }

    [Fact]
    public async Task StartAsync_ShouldLogAndStartPolling_WhenEnabled()
    {
        var sut = CreateSut();

        await sut.StartAsync(CancellationToken.None);

        _loggerMock.Verify(x =>
            x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("QueuePoller start requested.")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task StartAsync_ShouldSkipPolling_WhenDisabled()
    {
        _options.Disabled = true;
        var sut = CreateSut();

        await sut.StartAsync(CancellationToken.None);

        _loggerMock.Verify(x =>
            x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("disabled in config")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task StopAsync_ShouldLogAndCancelPolling()
    {
        var sut = CreateSut();
        await sut.StartAsync(CancellationToken.None);

        await sut.StopAsync(CancellationToken.None);

        _loggerMock.Verify(x =>
            x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("QueuePoller stop requested.")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task PollMessagesAsync_ShouldHandleOperationCanceledException_Gracefully()
    {
        var cancellationSource = new CancellationTokenSource();

        _sqsMock
            .Setup(x => x.ReceiveMessageAsync(It.IsAny<ReceiveMessageRequest>(), It.IsAny<CancellationToken>()))
            .Returns(async (ReceiveMessageRequest _, CancellationToken token) =>
            {
                await Task.Delay(100, token);
                token.ThrowIfCancellationRequested();
                return new ReceiveMessageResponse { Messages = [] };
            });

        var sut = CreateSut();

        await sut.StartAsync(cancellationSource.Token);

        await Task.Delay(50);
        cancellationSource.Cancel();

        await sut.StopAsync(CancellationToken.None);

        _loggerMock.Verify(x =>
            x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("QueuePoller stop requested.")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        Func<Task> act = async () => await sut.DisposeAsync();
        await act.Should().NotThrowAsync();
    }
}