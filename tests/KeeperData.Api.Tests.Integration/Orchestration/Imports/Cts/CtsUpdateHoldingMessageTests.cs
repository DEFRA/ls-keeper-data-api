using FluentAssertions;
using KeeperData.Api.Tests.Integration.Consumers.Helpers;
using KeeperData.Api.Tests.Integration.Helpers;
using KeeperData.Core.Messaging.Contracts.V1.Cts;
using KeeperData.Tests.Common.Generators;
using Xunit;

namespace KeeperData.Api.Tests.Integration.Orchestration.Imports.Cts;

[Collection("Integration"), Trait("Dependence", "testcontainers")]
public class CtsUpdateHoldingMessageTests(MongoDbFixture mongoDbFixture, LocalStackFixture localStackFixture, ApiContainerFixture apiContainerFixture)
{
    private readonly MongoDbFixture _mongoDbFixture = mongoDbFixture;
    private readonly LocalStackFixture _localStackFixture = localStackFixture;
    private readonly ApiContainerFixture _apiContainerFixture = apiContainerFixture;
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
        var additionalUserProperties = new Dictionary<string, string> { ["CorrelationId"] = correlationId };
        var request = SQSMessageUtility.CreateMessage(_localStackFixture.LsKeeperDataIntakeQueue, message, typeof(TMessage).Name, additionalUserProperties);
        await _localStackFixture.SqsClient.SendMessageAsync(request, CancellationToken.None);
    }
}