using FluentAssertions;
using KeeperData.Api.Tests.Integration.Consumers.Helpers;
using KeeperData.Api.Tests.Integration.Fixtures;
using KeeperData.Api.Tests.Integration.Helpers;
using KeeperData.Core.Messaging.Contracts.V1.Sam;
using KeeperData.Tests.Common.Generators;

namespace KeeperData.Api.Tests.Integration.Orchestration.Imports.Sam;

[Collection("Integration"), Trait("Dependence", "testcontainers")]
public class SamUpdateHoldingMessageTests(MongoDbFixture mongoDbFixture, LocalStackFixture localStackFixture, ApiContainerFixture apiContainerFixture)
{
    private readonly MongoDbFixture _mongoDbFixture = mongoDbFixture;
    private readonly LocalStackFixture _localStackFixture = localStackFixture;
    private readonly ApiContainerFixture _apiContainerFixture = apiContainerFixture;

    private const int ProcessingTimeCircuitBreakerSeconds = 10;

    [Fact]
    public async Task GivenSamUpdateHoldingMessagePublishedToQueue_WhenReceived_ShouldBeHandled()
    {
        var correlationId = Guid.NewGuid().ToString();
        var holdingIdentifier = CphGenerator.GenerateFormattedCph();
        var message = new SamUpdateHoldingMessage { Identifier = holdingIdentifier };

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
        var request = SQSMessageUtility.CreateMessage(_localStackFixture.KrdsIntakeQueueUrl, message, typeof(TMessage).Name, additionalUserProperties);
        await _localStackFixture.SqsClient.SendMessageAsync(request, CancellationToken.None);
    }
}