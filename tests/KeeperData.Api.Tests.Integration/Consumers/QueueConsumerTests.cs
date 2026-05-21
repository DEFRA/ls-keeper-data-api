using Amazon.SimpleNotificationService.Model;
using FluentAssertions;
using KeeperData.Api.Tests.Integration.Consumers.Helpers;
using KeeperData.Api.Tests.Integration.Fixtures;
using KeeperData.Api.Tests.Integration.Helpers;
using KeeperData.Core.Messaging.Contracts.V1.Cts;
using KeeperData.Tests.Common.Generators;

namespace KeeperData.Api.Tests.Integration.Consumers;

[Collection("Integration"), Trait("Dependence", "testcontainers")]
public class QueueConsumerTests(
    LocalStackFixture localStackFixture,
    ApiContainerFixture apiContainerFixture)
{
    private readonly LocalStackFixture _localStackFixture = localStackFixture;
    private readonly ApiContainerFixture _apiContainerFixture = apiContainerFixture;

    [Fact]
    public async Task GivenMessagePublishedToTopic_WhenReceivedOnTheQueue_ShouldComplete()
    {
        var correlationId = Guid.NewGuid().ToString();
        var holdingIdentifier = CphGenerator.GenerateFormattedCph();
        var message = GetCtsImportHoldingMessage(holdingIdentifier);

        await ExecuteTopicTest(correlationId, message);

        // Added polling loop
        bool foundMessageProcesseEntryInLogs = false;
        for (int i = 0; i < 15; i++)
        {
            foundMessageProcesseEntryInLogs = await ContainerLoggingUtility.FindContainerLogEntryAsync(
                _apiContainerFixture.ApiContainer,
                $"Handled message with correlationId: \"{correlationId}\"");

            if (foundMessageProcesseEntryInLogs)
            {
                break;
            }

            await Task.Delay(TimeSpan.FromSeconds(2));
        }

        foundMessageProcesseEntryInLogs.Should().BeTrue("The API container should successfully process the message and log its completion");
    }

    [Fact]
    public async Task GivenMessagePublishedToQueue_WhenReceivedOnTheQueue_ShouldComplete()
    {
        var correlationId = Guid.NewGuid().ToString();
        var holdingIdentifier = CphGenerator.GenerateFormattedCph();
        var message = GetCtsImportHoldingMessage(holdingIdentifier);

        await ExecuteQueueTest(correlationId, message);

        bool foundMessageProcesseEntryInLogs = false;
        for (int i = 0; i < 15; i++)
        {
            foundMessageProcesseEntryInLogs = await ContainerLoggingUtility.FindContainerLogEntryAsync(
                _apiContainerFixture.ApiContainer,
                $"Handled message with correlationId: \"{correlationId}\"");

            if (foundMessageProcesseEntryInLogs)
            {
                break;
            }

            await Task.Delay(TimeSpan.FromSeconds(2));
        }

        foundMessageProcesseEntryInLogs.Should().BeTrue("The API container should successfully process the message and log its completion");
    }

    private async Task ExecuteTopicTest<TMessage>(string correlationId, TMessage message)
    {
        var topic = new Topic { TopicArn = _localStackFixture.DataBridgeEventsTopicArn };
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
        var request = SQSMessageUtility.CreateMessage(_localStackFixture.KrdsIntakeQueueUrl!, message, typeof(TMessage).Name, additionalUserProperties);

        using var cts = new CancellationTokenSource();
        await _localStackFixture.SqsClient.SendMessageAsync(request, cts.Token);
    }

    private static CtsImportHoldingMessage GetCtsImportHoldingMessage(string holdingIdentifier) => new()
    {
        Identifier = holdingIdentifier
    };
}