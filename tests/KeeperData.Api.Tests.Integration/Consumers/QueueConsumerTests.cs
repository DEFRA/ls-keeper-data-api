using Amazon.SimpleNotificationService.Model;
using FluentAssertions;
using KeeperData.Api.Tests.Integration.Consumers.Contracts;
using KeeperData.Api.Tests.Integration.Consumers.Helpers;
using KeeperData.Api.Tests.Integration.Helpers;

namespace KeeperData.Api.Tests.Integration.Consumers;

[Trait("Category", "Integration")]
public class QueueConsumerTests(IntegrationTestFixture fixture) : IClassFixture<IntegrationTestFixture>
{
    [Fact]
    public async Task GivenMessagePublishedToTopic_WhenReceivedOnTheQueue_ShouldComplete()
    {
        var messageId = Guid.NewGuid().ToString();
        var messageText = Guid.NewGuid();
        var integrationTestMessage = GetIntegrationTestMessage(messageText.ToString());

        await ExecuteTest(messageId, integrationTestMessage);

        // Wait briefly to allow processing
        await Task.Delay(TimeSpan.FromSeconds(5));

        var foundMessageProcesseEntryInLogs = await ContainerLoggingUtility.FindContainerLogEntryAsync(
            ContainerLoggingUtility.ServiceNameApi,
            $"Handled message with CorrelationId: \"{messageId}\"");

        foundMessageProcesseEntryInLogs.Should().BeTrue();
    }

    private async Task ExecuteTest(string correlationId, IntegrationTestMessage message)
    {
        var topic = new Topic { TopicArn = "arn:aws:sns:eu-west-2:000000000000:ls-keeper-data-bridge-events" };
        var additionalUserProperties = new Dictionary<string, string> {
            ["CorrelationId"] = correlationId
        };
        var publishRequest = SNSMessageUtility.CreateMessage(topic.TopicArn, message, "Placeholder", additionalUserProperties);

        using var cts = new CancellationTokenSource();
        await fixture.PublishToTopicAsync(publishRequest, cts.Token);
    }

    private static IntegrationTestMessage GetIntegrationTestMessage(string message) => new()
    {
        Message = message
    };
}
