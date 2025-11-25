using FluentAssertions;
using KeeperData.Api.Tests.Integration.Consumers.Helpers;
using KeeperData.Api.Tests.Integration.Helpers;
using KeeperData.Core.Documents.Silver;
using KeeperData.Core.Domain.Sites.Formatters;
using KeeperData.Core.Messaging.Contracts.V1.Cts;
using KeeperData.Tests.Common.Generators;
using MongoDB.Driver;
using Xunit;

namespace KeeperData.Api.Tests.Integration.Orchestration.Updates.Cts;

[Trait("Dependence", "localstack")]
public class CtsUpdateHoldingMessageTests(IntegrationTestFixture fixture) : IClassFixture<IntegrationTestFixture>
{
    [Fact]
    public async Task GivenCtsUpdateHoldingMessagePublishedToQueue_WhenReceived_ShouldPersistSilverData()
    {
        // Arrange
        var correlationId = Guid.NewGuid().ToString();
        // Grabbing CPH from FakeDataBridgeClient
        var holdingIdentifier = CphGenerator.GenerateCtsFormattedLidIdentifier("AH");

        var message = new CtsUpdateHoldingMessage { Identifier = holdingIdentifier };

        // Act
        await ExecuteQueueTest(correlationId, message);

        // Assert
        var timeout = TimeSpan.FromSeconds(15);
        var pollInterval = TimeSpan.FromSeconds(1);
        var startTime = DateTime.UtcNow;

        List<CtsHoldingDocument> storedDocuments = [];

        // Loop till data shows
        while (DateTime.UtcNow - startTime < timeout)
        {
            var cphFilter = holdingIdentifier.LidIdentifierToCph();
            var filter = Builders<CtsHoldingDocument>.Filter.Eq(x => x.CountyParishHoldingNumber, cphFilter);

            storedDocuments = await fixture.MongoVerifier.FindDocumentsAsync("ctsHoldings", filter);

            if (storedDocuments.Count > 0) break;
            await Task.Delay(pollInterval);
        }

        // Verify we persisted the record
        storedDocuments.Should().NotBeEmpty("The CTS Holding document should have been persisted to the database via the update pipeline");
        storedDocuments[0].CountyParishHoldingNumber.Should().Be(holdingIdentifier.LidIdentifierToCph());
    }

    private async Task ExecuteQueueTest<TMessage>(string correlationId, TMessage message)
    {
        var queueUrl = "http://sqs.eu-west-2.127.0.0.1:4566/000000000000/ls_keeper_data_intake_queue";
        var additionalUserProperties = new Dictionary<string, string> { ["CorrelationId"] = correlationId };
        var request = SQSMessageUtility.CreateMessage(queueUrl, message, typeof(TMessage).Name, additionalUserProperties);
        await fixture.PublishToQueueAsync(request, CancellationToken.None);
    }
}