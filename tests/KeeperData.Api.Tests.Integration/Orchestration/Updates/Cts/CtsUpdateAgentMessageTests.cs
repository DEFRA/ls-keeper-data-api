using FluentAssertions;
using KeeperData.Api.Tests.Integration.Consumers.Helpers;
using KeeperData.Api.Tests.Integration.Helpers;
using KeeperData.Core.Documents.Silver;
using KeeperData.Core.Messaging.Contracts.V1.Cts;
using MongoDB.Driver;

namespace KeeperData.Api.Tests.Integration.Orchestration.Updates.Cts;

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
    public async Task GivenCtsUpdateAgentMessagePublishedToQueue_WhenReceived_ShouldPersistSilverData()
    {
        var correlationId = Guid.NewGuid().ToString();

        var partyId = Guid.NewGuid().ToString();
        var message = new CtsUpdateAgentMessage { Identifier = partyId };

        var beforetest = DateTime.UtcNow;
        await ExecuteQueueTest(correlationId, message);

        var timeout = TimeSpan.FromSeconds(ProcessingTimeCircuitBreakerSeconds);
        var pollInterval = TimeSpan.FromSeconds(1);
        var startTime = DateTime.UtcNow;

        List<CtsPartyDocument> storedDocuments = [];

        while (DateTime.UtcNow - startTime < timeout)
        {
            var filter = Builders<CtsPartyDocument>.Filter.Eq(x => x.PartyId, partyId);
            storedDocuments = await _mongoDbFixture.MongoVerifier.FindDocumentsAsync("ctsParties", filter);

            if (storedDocuments.Count > 0) break;
            await Task.Delay(pollInterval);
        }

        storedDocuments.Should().NotBeEmpty();
        storedDocuments[0].PartyId.Should().Be(partyId);

        var foundLogEntry = await ContainerLoggingUtility.FindContainerLogEntryAsync(
            ContainerLoggingUtility.ServiceNameApi,
            $"Handled message with correlationId: \"{correlationId}\"");
        foundLogEntry.Should().BeTrue();
    }

    private async Task ExecuteQueueTest<TMessage>(string correlationId, TMessage message)
    {
        var additionalUserProperties = new Dictionary<string, string> { ["CorrelationId"] = correlationId };
        var request = SQSMessageUtility.CreateMessage(_localStackFixture.LsKeeperDataIntakeQueue, message, typeof(TMessage).Name, additionalUserProperties);
        await _localStackFixture.SqsClient.SendMessageAsync(request, CancellationToken.None);
    }
}