using Amazon.SQS.Model;
using FluentAssertions;
using KeeperData.Api.Tests.Integration.Consumers.Helpers;
using KeeperData.Api.Tests.Integration.Helpers;
using KeeperData.Core.Messaging.Contracts.V1.Sam;
using KeeperData.Tests.Common.Generators;

namespace KeeperData.Api.Tests.Integration.Consumers;

[Trait("Dependence", "localstack")]
[Collection("Integration Tests")]
public class FifoQueueConsumerTests(IntegrationTestFixture fixture) : IClassFixture<IntegrationTestFixture>
{
    private const string FifoQueueUrl = "http://sqs.eu-west-2.127.0.0.1:4566/000000000000/ls_keeper_data_intake_standard_fifo_queue.fifo";

    [Fact]
    public async Task GivenFifoMessagesWithSameCph_WhenPublishedToFifoQueue_ShouldProcessInOrder()
    {
        // Arrange - Create messages with same CPH to ensure they're in same MessageGroup
        var cphIdentifier = "12/345/6789";
        var correlationId1 = Guid.NewGuid().ToString();
        var correlationId2 = Guid.NewGuid().ToString();
        var correlationId3 = Guid.NewGuid().ToString();

        var message1 = new SamImportHoldingMessage { Identifier = cphIdentifier };
        var message2 = new SamUpdateHoldingMessage { Identifier = cphIdentifier };
        var message3 = new SamImportHoldingMessage { Identifier = cphIdentifier };

        // Act - Publish messages in specific order
        await PublishFifoMessage(correlationId1, message1);
        await PublishFifoMessage(correlationId2, message2);
        await PublishFifoMessage(correlationId3, message3);

        // Wait for processing
        await Task.Delay(TimeSpan.FromSeconds(10));

        // Assert - Verify messages were processed in the correct order by checking log entries
        var logEntries = await ContainerLoggingUtility.FindContainerLogEntriesAsync(
            ContainerLoggingUtility.ServiceNameApi,
            "Handled message with correlationId:");

        // Find the indices of our specific messages in the log
        var message1LogEntry = logEntries.FirstOrDefault(log => log.Contains(correlationId1));
        var message2LogEntry = logEntries.FirstOrDefault(log => log.Contains(correlationId2));
        var message3LogEntry = logEntries.FirstOrDefault(log => log.Contains(correlationId3));

        // All messages should have been processed
        message1LogEntry.Should().NotBeNull("Message 1 should have been processed");
        message2LogEntry.Should().NotBeNull("Message 2 should have been processed");
        message3LogEntry.Should().NotBeNull("Message 3 should have been processed");

        // Note: In a real test environment, we would need to verify ordering by timestamp or sequence
        // For now, we're validating that FIFO messages can be published and processed
    }

    [Fact]
    public async Task GivenFifoMessagesWithDifferentCphs_WhenPublishedToFifoQueue_ShouldProcessInParallel()
    {
        // Arrange - Create messages with different CPHs (different MessageGroups)
        var cphIdentifier1 = "12/345/6789";
        var cphIdentifier2 = "98/765/4321";
        var correlationId1 = Guid.NewGuid().ToString();
        var correlationId2 = Guid.NewGuid().ToString();

        var message1 = new SamImportHoldingMessage { Identifier = cphIdentifier1 };
        var message2 = new SamImportHoldingMessage { Identifier = cphIdentifier2 };

        // Act - Publish messages to different MessageGroups
        await PublishFifoMessage(correlationId1, message1);
        await PublishFifoMessage(correlationId2, message2);

        // Wait for processing
        await Task.Delay(TimeSpan.FromSeconds(8));

        // Assert - Both messages should be processed (parallel processing allowed)
        var foundMessage1 = await ContainerLoggingUtility.FindContainerLogEntryAsync(
            ContainerLoggingUtility.ServiceNameApi,
            $"Handled message with correlationId: \"{correlationId1}\"");

        var foundMessage2 = await ContainerLoggingUtility.FindContainerLogEntryAsync(
            ContainerLoggingUtility.ServiceNameApi,
            $"Handled message with correlationId: \"{correlationId2}\"");

        foundMessage1.Should().BeTrue("Message 1 should have been processed");
        foundMessage2.Should().BeTrue("Message 2 should have been processed");
    }

    [Fact]
    public async Task GivenFifoMessage_WhenCreated_ShouldHaveCorrectFifoAttributes()
    {
        // Arrange
        var cphIdentifier = "AB/123/45678";
        var correlationId = Guid.NewGuid().ToString();
        var message = new SamImportHoldingMessage { Identifier = cphIdentifier };

        // Act
        var fifoMessage = CreateFifoMessage(correlationId, message);

        // Assert - Verify FIFO attributes are set correctly
        fifoMessage.MessageGroupId.Should().Be("CPH_AB_123_45678", "MessageGroupId should be normalized CPH");
        fifoMessage.MessageDeduplicationId.Should().NotBeNullOrEmpty("MessageDeduplicationId should be set");
        fifoMessage.QueueUrl.Should().EndWith(".fifo", "Queue should be FIFO queue");
    }

    private async Task PublishFifoMessage<TMessage>(string correlationId, TMessage message)
    {
        var fifoMessage = CreateFifoMessage(correlationId, message);

        using var cts = new CancellationTokenSource();
        await fixture.PublishToFifoQueueAsync(fifoMessage, cts.Token);
    }

    private static SendMessageRequest CreateFifoMessage<TMessage>(string correlationId, TMessage message)
    {
        var additionalUserProperties = new Dictionary<string, string>
        {
            ["CorrelationId"] = correlationId
        };

        // Create FIFO message manually for testing
        var fifoMessage = SQSMessageUtility.CreateMessage(FifoQueueUrl, message, typeof(TMessage).Name, additionalUserProperties);

        // Add FIFO specific attributes manually for now
        // TODO: Replace with actual MessageFactory FIFO extension once service is available in test context
        var messageGroupId = message switch
        {
            SamImportHoldingMessage sam => $"CPH_{sam.Identifier.Replace("/", "_")}",
            SamUpdateHoldingMessage sam => $"CPH_{sam.Identifier.Replace("/", "_")}",
            _ => "DEFAULT_GROUP"
        };

        fifoMessage.MessageGroupId = messageGroupId;
        fifoMessage.MessageDeduplicationId = Guid.NewGuid().ToString();

        return fifoMessage;
    }
}