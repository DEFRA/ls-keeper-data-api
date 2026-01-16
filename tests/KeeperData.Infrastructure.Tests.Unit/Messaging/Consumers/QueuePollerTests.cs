using Amazon.SQS;
using Amazon.SQS.Model;
using FluentAssertions;
using KeeperData.Application.Commands.MessageProcessing;
using KeeperData.Core.Messaging.Contracts;
using KeeperData.Core.Messaging.Contracts.V1.Sam;
using KeeperData.Core.Messaging.Observers;
using KeeperData.Core.Messaging.Serializers;
using KeeperData.Core.Messaging.Throttling;
using KeeperData.Core.Providers;
using KeeperData.Infrastructure.Messaging.Configuration;
using KeeperData.Infrastructure.Messaging.Consumers;
using KeeperData.Infrastructure.Messaging.Factories.Implementations;
using KeeperData.Infrastructure.Messaging.Services;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Text.Json;

namespace KeeperData.Infrastructure.Tests.Unit.Messaging.Consumers;

public class QueuePollerTests
{
    private readonly Mock<IServiceScopeFactory> _scopeFactoryMock = new();
    private readonly Mock<IAmazonSQS> _sqsMock = new();
    private readonly Mock<IMessageSerializer<SnsEnvelope>> _serializerMock = new();
    private readonly Mock<ILogger<QueuePoller>> _loggerMock = new();
    private readonly Mock<IDeadLetterQueueService> _deadLetterQueueServiceMock = new();
    private readonly MessageCommandRegistry _messageCommandRegistry = new();
    private readonly Mock<IDataImportThrottlingConfiguration> _dataImportThrottlingConfigurationMock = new();
    private readonly Mock<IDelayProvider> _delayProviderMock = new();

    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly Mock<IServiceProvider> _serviceProviderMock = new();
    private readonly Mock<IServiceScope> _scopeMock = new();

    private readonly IntakeEventQueueOptions _options = new()
    {
        QueueUrl = "http://localhost:4566/000000000000/test-queue",
        WaitTimeSeconds = 1,
        MaxNumberOfMessages = 1,
        Disabled = false
    };

    private QueuePoller CreateSut()
    {
        _messageCommandRegistry.Register<SamImportHoldingCommandFactory>("SamImportHolding");

        return new QueuePoller(
            _scopeFactoryMock.Object,
            _sqsMock.Object,
            _serializerMock.Object,
            _deadLetterQueueServiceMock.Object,
            _messageCommandRegistry,
            _dataImportThrottlingConfigurationMock.Object,
            Options.Create(_options),
            _delayProviderMock.Object,
            Mock.Of<IQueuePollerObserver<MessageType>>(),
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

    [Theory]
    [InlineData(0, false)]
    [InlineData(500, true)]
    [InlineData(1000, true)]
    public async Task PollMessagesAsync_WhenMessageCompletionDelayMs_ShouldThrottleMessageProcessing(int delayMs, bool expectDelay)
    {
        var cancellationSource = new CancellationTokenSource();

        var samImportHoldingMessage = new SamImportHoldingMessage { Identifier = Guid.NewGuid().ToString() };
        var samImportHoldingMessageSerialized = JsonSerializer.Serialize(samImportHoldingMessage, JsonDefaults.DefaultOptionsWithStringEnumConversion);

        var messageHandled = new TaskCompletionSource();

        _sqsMock
            .Setup(x => x.ReceiveMessageAsync(It.IsAny<ReceiveMessageRequest>(), It.IsAny<CancellationToken>()))
            .Returns(async (ReceiveMessageRequest _, CancellationToken token) =>
            {
                await Task.Delay(100, token);
                token.ThrowIfCancellationRequested();
                return GetReceiveMessageResponseArgs(samImportHoldingMessageSerialized);
            });

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<ProcessSamImportHoldingMessageCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(samImportHoldingMessage)
            .Callback(() => messageHandled.TrySetResult());

        _serviceProviderMock
            .Setup(sp => sp.GetService(typeof(IMediator)))
            .Returns(_mediatorMock.Object);

        _scopeMock
            .Setup(s => s.ServiceProvider)
            .Returns(_serviceProviderMock.Object);

        _scopeFactoryMock
            .Setup(f => f.CreateScope())
            .Returns(_scopeMock.Object);

        _dataImportThrottlingConfigurationMock
            .SetupGet(x => x.MessageCompletionDelayMs)
            .Returns(delayMs);

        _serializerMock
            .Setup(s => s.Deserialize(It.IsAny<Message>()))
            .Returns(new SnsEnvelope { Type = "" });

        var sut = CreateSut();

        await sut.StartAsync(cancellationSource.Token);

        await messageHandled.Task.WaitAsync(TimeSpan.FromSeconds(5));

        cancellationSource.Cancel();

        await sut.StopAsync(CancellationToken.None);

        _delayProviderMock.Verify(d => d.DelayAsync(TimeSpan.FromMilliseconds(delayMs), It.IsAny<CancellationToken>()),
            expectDelay ? Times.AtLeastOnce : Times.Never);

        await sut.DisposeAsync();
    }

    private static ReceiveMessageResponse GetReceiveMessageResponseArgs(string payload)
    {
        var messageArgs = BuildSqsMessage(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), typeof(SamImportHoldingMessage).Name, payload);
        var receiveMessageResponseArgs = new ReceiveMessageResponse { HttpStatusCode = System.Net.HttpStatusCode.OK, Messages = [messageArgs] };
        return receiveMessageResponseArgs;
    }

    private static Message BuildSqsMessage(string messageId, string correlationId, string subject, string payload)
    {
        return new Message
        {
            MessageId = messageId,
            Body = payload,
            MessageAttributes = new Dictionary<string, MessageAttributeValue>
            {
                ["Subject"] = new() { DataType = "String", StringValue = subject },
                ["CorrelationId"] = new() { DataType = "String", StringValue = correlationId }
            }
        };
    }
}