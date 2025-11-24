using FluentAssertions;
using KeeperData.Api.Tests.Integration.Consumers.Helpers;
using KeeperData.Api.Tests.Integration.Helpers;
using KeeperData.Core.Messaging.Contracts.V1.Cts;
using KeeperData.Tests.Common.Generators;

namespace KeeperData.Api.Tests.Integration.Orchestration.Imports.Cts;

[Trait("Dependence", "localstack")]
public class CtsUpdateHoldingMessageTests(IntegrationTestFixture fixture) : IClassFixture<IntegrationTestFixture>
{
    private const int ProcessingTimeCircuitBreakerSeconds = 10;

    [Fact]
    public async Task GivenCtsUpdateHoldingMessagePublishedToQueue_WhenReceived_ShouldBeHandled()
    {
        var correlationId = Guid.NewGuid().ToString();
        var holdingIdentifier = CphGenerator.GenerateCtsFormattedLidIdentifier("AH");
        var message = new CtsUpdateHoldingMessage { Identifier = holdingIdentifier };

        await ExecuteQueueTest(correlationId, message);

        var timeout = TimeSpan.FromSeconds(ProcessingTimeCircuitBreakerSeconds);
        var pollInterval = TimeSpan.FromSeconds(2);

        var foundLogEntry = false;
        var startTime = DateTime.UtcNow;
        while (DateTime.UtcNow - startTime < timeout)
        {
            foundLogEntry = await ContainerLoggingUtility.FindContainerLogEntryAsync(
                ContainerLoggingUtility.ServiceNameApi,
                $"Handled message with correlationId: \"{correlationId}\"");

            if (foundLogEntry) break;
            await Task.Delay(pollInterval);
        }

        foundLogEntry.Should().BeTrue();
    }

    private async Task ExecuteQueueTest<TMessage>(string correlationId, TMessage message)
    {
        var queueUrl = "http://sqs.eu-west-2.127.0.0.1:4566/000000000000/ls_keeper_data_intake_queue";
        var additionalUserProperties = new Dictionary<string, string> { ["CorrelationId"] = correlationId };
        var request = SQSMessageUtility.CreateMessage(queueUrl, message, typeof(TMessage).Name, additionalUserProperties);
        await fixture.PublishToQueueAsync(request, CancellationToken.None);
    }
}