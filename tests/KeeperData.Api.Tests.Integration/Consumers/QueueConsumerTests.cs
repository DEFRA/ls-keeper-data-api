using Amazon.SimpleNotificationService.Model;
using FluentAssertions;
using KeeperData.Api.Tests.Integration.Consumers.Helpers;
using KeeperData.Api.Tests.Integration.Helpers;
using KeeperData.Core.Messaging.Contracts.V1.Cts;
using KeeperData.Tests.Common.Generators;

namespace KeeperData.Api.Tests.Integration.Consumers;

[Collection("Integration"), Trait("Dependence", "testcontainers")]
public class QueueConsumerTests(MongoDbFixture mongoDbFixture, LocalStackFixture localStackFixture, ApiContainerFixture apiContainerFixture)
{
    private readonly MongoDbFixture _mongoDbFixture = mongoDbFixture;
    private readonly LocalStackFixture _localStackFixture = localStackFixture;
    private readonly ApiContainerFixture _apiContainerFixture = apiContainerFixture;

    [Fact]
    public async Task GivenMessagePublishedToTopic_WhenReceivedOnTheQueue_ShouldComplete()
    {
        var correlationId = Guid.NewGuid().ToString();
        var holdingIdentifier = CphGenerator.GenerateFormattedCph();
        var message = GetCtsImportHoldingMessage(holdingIdentifier);

        await ExecuteTopicTest(correlationId, message);

        // Wait briefly to allow processing
        await Task.Delay(TimeSpan.FromSeconds(5));

        var foundMessageProcesseEntryInLogs = await ContainerLoggingUtility.FindContainerLogEntryAsync(
            ContainerLoggingUtility.ServiceNameApi,
            $"Handled message with correlationId: \"{correlationId}\"");

        foundMessageProcesseEntryInLogs.Should().BeTrue();
    }

    [Fact]
    public async Task GivenMessagePublishedToQueue_WhenReceivedOnTheQueue_ShouldComplete()
    {
        var correlationId = Guid.NewGuid().ToString();
        var holdingIdentifier = CphGenerator.GenerateFormattedCph();
        var message = GetCtsImportHoldingMessage(holdingIdentifier);

        await ExecuteQueueTest(correlationId, message);

        // Wait briefly to allow processing
        await Task.Delay(TimeSpan.FromSeconds(5));

        var foundMessageProcesseEntryInLogs = await ContainerLoggingUtility.FindContainerLogEntryAsync(
            ContainerLoggingUtility.ServiceNameApi,
            $"Handled message with correlationId: \"{correlationId}\"");

        foundMessageProcesseEntryInLogs.Should().BeTrue();
    }

    private async Task ExecuteTopicTest<TMessage>(string correlationId, TMessage message)
    {
        var topic = new Topic { TopicArn = _localStackFixture.TopicArn };
        var additionalUserProperties = new Dictionary<string, string>
        {
            ["CorrelationId"] = correlationId
        };
        var request = SNSMessageUtility.CreateMessage(topic.TopicArn ?? "", message, typeof(TMessage).Name, additionalUserProperties);

        using var cts = new CancellationTokenSource();
        await _localStackFixture.PublishToTopicAsync(request, cts.Token);
    }

    private async Task ExecuteQueueTest<TMessage>(string correlationId, TMessage message)
    {
        var additionalUserProperties = new Dictionary<string, string>
        {
            ["CorrelationId"] = correlationId
        };
        var request = SQSMessageUtility.CreateMessage(_localStackFixture.LsKeeperDataIntakeQueue, message, typeof(TMessage).Name, additionalUserProperties);

        using var cts = new CancellationTokenSource();
        await _localStackFixture.SqsClient.SendMessageAsync(request, cts.Token);
    }

    private static CtsImportHoldingMessage GetCtsImportHoldingMessage(string holdingIdentifier) => new()
    {
        Identifier = holdingIdentifier
    };
}