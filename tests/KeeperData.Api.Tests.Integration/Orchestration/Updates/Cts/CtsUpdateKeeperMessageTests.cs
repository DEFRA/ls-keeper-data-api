using FluentAssertions;
using KeeperData.Api.Tests.Integration.Consumers.Helpers;
using KeeperData.Api.Tests.Integration.Fixtures;
using KeeperData.Api.Tests.Integration.Helpers;
using KeeperData.Core.Documents.Silver;
using KeeperData.Core.Messaging.Contracts.V1.Cts;
using MongoDB.Driver;

namespace KeeperData.Api.Tests.Integration.Orchestration.Updates.Cts;

[Collection("Integration"), Trait("Dependence", "testcontainers")]
public class CtsUpdateKeeperMessageTests(
    MongoDbFixture mongoDbFixture,
    LocalStackFixture localStackFixture,
    ApiContainerFixture apiContainerFixture) : IAsyncLifetime
{
    private readonly MongoDbFixture _mongoDbFixture = mongoDbFixture;
    private readonly LocalStackFixture _localStackFixture = localStackFixture;
    private readonly ApiContainerFixture _apiContainerFixture = apiContainerFixture;

    private const int ProcessingTimeCircuitBreakerSeconds = 10;

    [Fact]
    public async Task GivenCtsUpdateKeeperMessagePublishedToQueue_WhenReceived_ShouldPersistSilverData()
    {
        var correlationId = Guid.NewGuid().ToString();
        var partyId = Guid.NewGuid().ToString();
        var message = new CtsUpdateKeeperMessage { Identifier = partyId };

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
            _apiContainerFixture.ApiContainer,
            $"Handled message with correlationId: \"{correlationId}\"");
        foundLogEntry.Should().BeTrue();
    }

    private async Task ExecuteQueueTest<TMessage>(string correlationId, TMessage message)
    {
        var additionalUserProperties = new Dictionary<string, string> { ["CorrelationId"] = correlationId };
        var request = SQSMessageUtility.CreateMessage(_localStackFixture.KrdsIntakeQueueUrl!, message, typeof(TMessage).Name, additionalUserProperties);
        await _localStackFixture.SqsClient.SendMessageAsync(request, CancellationToken.None);
    }

    public async Task InitializeAsync()
    {
        await Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _mongoDbFixture.PurgeDataTables();
    }
}