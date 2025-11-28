using FluentAssertions;
using KeeperData.Api.Tests.Integration.Consumers.Helpers;
using KeeperData.Api.Tests.Integration.Helpers;
using KeeperData.Core.Documents.Silver;
using KeeperData.Core.Messaging.Contracts.V1.Cts;
using MongoDB.Driver;
using Xunit;

namespace KeeperData.Api.Tests.Integration.Orchestration.Imports.Cts;

[Trait("Dependence", "localstack")]
public class CtsUpdateKeeperMessageTests(IntegrationTestFixture fixture) : IClassFixture<IntegrationTestFixture>
{
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
            storedDocuments = await fixture.MongoVerifier.FindDocumentsAsync("ctsParties", filter);

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
        var queueUrl = "http://sqs.eu-west-2.127.0.0.1:4566/000000000000/ls_keeper_data_intake_queue";
        var additionalUserProperties = new Dictionary<string, string> { ["CorrelationId"] = correlationId };
        var request = SQSMessageUtility.CreateMessage(queueUrl, message, typeof(TMessage).Name, additionalUserProperties);
        await fixture.PublishToQueueAsync(request, CancellationToken.None);
    }
}