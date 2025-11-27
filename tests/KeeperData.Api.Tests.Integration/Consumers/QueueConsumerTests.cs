using Amazon.SimpleNotificationService.Model;
using FluentAssertions;
using KeeperData.Api.Tests.Integration.Consumers.Helpers;
using KeeperData.Api.Tests.Integration.Helpers;
using KeeperData.Core.Messaging.Contracts.V1.Cts;
using KeeperData.Tests.Common.Generators;

namespace KeeperData.Api.Tests.Integration.Consumers;

[Trait("Dependence", "localstack")]
[Collection("Integration Tests")]
public class QueueConsumerTests(IntegrationTestFixture fixture) : IClassFixture<IntegrationTestFixture>
{
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
        var topic = new Topic { TopicArn = "arn:aws:sns:eu-west-2:000000000000:ls-keeper-data-bridge-events" };
        var additionalUserProperties = new Dictionary<string, string>
        {
            ["CorrelationId"] = correlationId
        };
        var request = SNSMessageUtility.CreateMessage(topic.TopicArn, message, typeof(TMessage).Name, additionalUserProperties);

        using var cts = new CancellationTokenSource();
        await fixture.PublishToTopicAsync(request, cts.Token);
    }

    private async Task ExecuteQueueTest<TMessage>(string correlationId, TMessage message)
    {
        var queueUrl = "http://sqs.eu-west-2.127.0.0.1:4566/000000000000/ls_keeper_data_intake_queue";
        var additionalUserProperties = new Dictionary<string, string>
        {
            ["CorrelationId"] = correlationId
        };
        var request = SQSMessageUtility.CreateMessage(queueUrl, message, typeof(TMessage).Name, additionalUserProperties);

        using var cts = new CancellationTokenSource();
        await fixture.PublishToQueueAsync(request, cts.Token);
    }

    private static CtsImportHoldingMessage GetCtsImportHoldingMessage(string holdingIdentifier) => new()
    {
        Identifier = holdingIdentifier
    };
}