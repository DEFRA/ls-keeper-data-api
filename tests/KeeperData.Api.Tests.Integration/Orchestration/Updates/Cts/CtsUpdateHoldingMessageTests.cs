using FluentAssertions;
using KeeperData.Api.Tests.Integration.Consumers.Helpers;
using KeeperData.Api.Tests.Integration.Fixtures;
using KeeperData.Api.Tests.Integration.Helpers;
using KeeperData.Core.Documents.Silver;
using KeeperData.Core.Domain.Sites.Formatters;
using KeeperData.Core.Messaging.Contracts.V1.Cts;
using KeeperData.Tests.Common.Generators;
using MongoDB.Driver;

namespace KeeperData.Api.Tests.Integration.Orchestration.Updates.Cts;

[Collection("Integration"), Trait("Dependence", "testcontainers")]
public class CtsUpdateHoldingMessageTests(
    MongoDbFixture mongoDbFixture,
    LocalStackFixture localStackFixture,
    ApiContainerFixture apiContainerFixture) : IAsyncLifetime
{
    private readonly MongoDbFixture _mongoDbFixture = mongoDbFixture;
    private readonly LocalStackFixture _localStackFixture = localStackFixture;
    private readonly ApiContainerFixture _apiContainerFixture = apiContainerFixture;

    private const int ProcessingTimeCircuitBreakerSeconds = 10;

    [Fact]
    public async Task GivenCtsUpdateHoldingMessagePublishedToQueue_WhenReceived_ShouldPersistSilverData()
    {
        // Arrange
        var correlationId = Guid.NewGuid().ToString();
        var holdingIdentifier = CphGenerator.GenerateCtsFormattedLidIdentifier("AH");
        var message = new CtsUpdateHoldingMessage { Identifier = holdingIdentifier };

        // Act
        await ExecuteQueueTest(correlationId, message);

        // Assert
        var timeout = TimeSpan.FromSeconds(ProcessingTimeCircuitBreakerSeconds);
        var pollInterval = TimeSpan.FromSeconds(1);
        var startTime = DateTime.UtcNow;

        List<CtsHoldingDocument> storedDocuments = [];

        // Wait for data to appear in Mongo
        while (DateTime.UtcNow - startTime < timeout)
        {
            var filter = Builders<CtsHoldingDocument>.Filter.Eq(x => x.CountyParishHoldingNumber, holdingIdentifier.LidIdentifierToCph());
            storedDocuments = await _mongoDbFixture.MongoVerifier.FindDocumentsAsync("ctsHoldings", filter);

            if (storedDocuments.Count > 0) break;
            await Task.Delay(pollInterval);
        }

        storedDocuments.Should().NotBeEmpty("The CTS Holding document should have been persisted to the database");
        storedDocuments.Should().Contain(x => x.CountyParishHoldingNumber == holdingIdentifier.LidIdentifierToCph());

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