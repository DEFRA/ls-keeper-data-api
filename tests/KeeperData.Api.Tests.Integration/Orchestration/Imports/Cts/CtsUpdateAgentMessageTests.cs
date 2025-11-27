using FluentAssertions;
using KeeperData.Api.Tests.Integration.Consumers.Helpers;
using KeeperData.Api.Tests.Integration.Helpers;
using KeeperData.Core.Messaging.Contracts.V1.Cts;
using Xunit;

namespace KeeperData.Api.Tests.Integration.Orchestration.Imports.Cts;

[Collection("Integration"), Trait("Dependence", "testcontainers")]
public class CtsUpdateAgentMessageTests
{
    private const int ProcessingTimeCircuitBreakerSeconds = 10;
    private readonly MongoDbFixture _mongoDbFixture;
    private readonly LocalStackFixture _localStackFixture;
    private readonly ApiContainerFixture _apiContainerFixture;

    public CtsUpdateAgentMessageTests(MongoDbFixture mongoDbFixture, LocalStackFixture localStackFixture, ApiContainerFixture apiContainerFixture)
    {
        _mongoDbFixture = mongoDbFixture;
        _localStackFixture = localStackFixture;
        _apiContainerFixture = apiContainerFixture;
    }

    [Fact]
    public async Task GivenCtsUpdateAgentMessagePublishedToQueue_WhenReceived_ShouldBeHandled()
    {
        var correlationId = Guid.NewGuid().ToString();
        var message = new CtsUpdateAgentMessage { Identifier = "AGENT456" };

        await ExecuteQueueTest(correlationId, message);

        var timeout = TimeSpan.FromSeconds(ProcessingTimeCircuitBreakerSeconds);
        var pollInterval = TimeSpan.FromSeconds(2);

        var foundLogEntry = false;
        var startTime = DateTime.UtcNow;
        while (DateTime.UtcNow - startTime < timeout)
        {
            foundLogEntry = await ContainerLoggingUtilityFixture.FindContainerLogEntryAsync(
                _apiContainerFixture.ApiContainer,
                $"Handled message with correlationId: \"{correlationId}\"");

            if (foundLogEntry) break;
            await Task.Delay(pollInterval);
        }

        foundLogEntry.Should().BeTrue();
    }

    private async Task ExecuteQueueTest<TMessage>(string correlationId, TMessage message)
    {
        var queueUrl = $"{_localStackFixture.SqsEndpoint}/000000000000/ls_keeper_data_intake_queue";
        var additionalUserProperties = new Dictionary<string, string> { ["CorrelationId"] = correlationId };
        var request = SQSMessageUtility.CreateMessage(queueUrl, message, typeof(TMessage).Name, additionalUserProperties);
        await _localStackFixture.SqsClient.SendMessageAsync(request, CancellationToken.None);
    }
}