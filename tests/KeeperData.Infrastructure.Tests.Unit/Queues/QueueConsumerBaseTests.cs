using Amazon.SQS;
using Amazon.SQS.Model;
using KeeperData.Infrastructure.Queues;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace KeeperData.Infrastructure.Test.Queues;

public class QueueConsumerBaseTests
{
    [Fact]
    public async Task StartAsync_ShouldLogStart_ThenEnterExecutionLoop()
    {
        // Arrange
        var mockSetup = CreateMocks();
        var token = new CancellationToken();
        var sut = new ConsumerBaseTestHarness(mockSetup);

        // Act
        await sut.StartAsync(token);

        // Assert
        mockSetup.Logger.Received(1).LogInformation("QueueConsumerBase Service started.");
        await mockSetup.SqsClient.Received(1)
            .ReceiveMessageAsync(Arg.Any<ReceiveMessageRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StopAsync_ShouldLogStop_ThenReturnCompletedTask()
    {
        // Arrange
        var mockSetup = CreateMocks();
        var token = new CancellationToken();
        var sut = new ConsumerBaseTestHarness(mockSetup);

        // Act
        await sut.StopAsync(token);

        // Assert
        mockSetup.Logger.Received(1).LogInformation("QueueConsumerBase Service stopping.");
    }

    [Fact]
    public async Task SettingCancellationToken_StopExecutionLoop()
    {
        // Arrange
        var mockSetup = CreateMocks();
        AddResponses(mockSetup, 500);

        var source = new CancellationTokenSource();
        var token = source.Token;
        int receivedCalls = 0;

        // Act
        using (var sut = new ConsumerBaseTestHarness(mockSetup))
        {
            await sut.StartAsync(token);
            Thread.Sleep(1400);
            await source.CancelAsync();
            Thread.Sleep(1600);
            await sut.StopAsync(token);
            receivedCalls = sut.ReceivedMessages.Count();
        }

        // Assert
        receivedCalls.Should().Be(3);
    }

    [Fact]
    public async Task Dispose_ShouldCallDisposeOnSnsService()
    {
        // Arrange
        var mockSetup = CreateMocks();
        var token = new CancellationToken();

        // Act
        using (var sut = new ConsumerBaseTestHarness(mockSetup))
        {
            await sut.StartAsync(token);
            await sut.StopAsync(token);
        }

        // Assert
        mockSetup.SqsClient.Received(1).Dispose();
    }

    [Fact]
    public async Task ExecutionLoop_ShouldCallProcessMessageOnReceivedMessage()
    {
        // Arrange
        var mockSetup = CreateMocks();
        AddResponses(mockSetup, 500);

        var token = new CancellationToken();
        int receivedCalls = 0;

        // Act
        using (var sut = new ConsumerBaseTestHarness(mockSetup))
        {
            await sut.StartAsync(token);
            Thread.Sleep(1600);
            await sut.StopAsync(token);
            receivedCalls = sut.ReceivedMessages.Count();
        }

        // Assert
        receivedCalls.Should().Be(3);
    }

    public record TestModel(string TestString, int TestInt);

    private static HarnessSetup CreateMocks()
    {
        var logger = Substitute.For<ILogger<QueueConsumerBase<TestModel>>>();
        var sqsClient = Substitute.For<IAmazonSQS>();
        var options = Substitute.For<IOptions<QueueConsumerOptions>>();
        options.Value.Returns(new QueueConsumerOptions()
        {
            QueueUrl = string.Empty
        });

        return new HarnessSetup(logger, sqsClient, options);
    }

    private static void AddResponses(HarnessSetup harnessSetup, int delay)
    {
        harnessSetup.SqsClient
            .ReceiveMessageAsync(Arg.Any<ReceiveMessageRequest>(), Arg.Any<CancellationToken>())
            .Returns(x =>
                {
                    Thread.Sleep(delay);
                    return Task.FromResult(new ReceiveMessageResponse()
                    {
                        Messages = new List<Message>()
                        {
                            new Message()
                            {
                                Body = "{ \"TestString\": \"What is the answer to the Ultimate Question of Life, The Universe, and Everything?\", \"TestInt\": 42 }",
                            }
                        }
                    });
                });
    }

    public record HarnessSetup(
        ILogger<QueueConsumerBase<TestModel>> Logger,
        IAmazonSQS SqsClient,
        IOptions<QueueConsumerOptions> Options);

    public class ConsumerBaseTestHarness(HarnessSetup setup) : QueueConsumerBase<TestModel>(setup.Logger, setup.SqsClient, setup.Options)
    {
        public List<TestModel> ReceivedMessages = new();
        protected override Task ProcessMessageAsync(TestModel payload, CancellationToken cancellationToken)
        {
            ReceivedMessages.Add(payload);
            return Task.CompletedTask;
        }
    }
}