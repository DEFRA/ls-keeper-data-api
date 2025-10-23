using FluentAssertions;
using KeeperData.Api.Tests.Integration.Consumers.Helpers;
using KeeperData.Api.Tests.Integration.Helpers;
using KeeperData.Core.Documents.Silver;
using KeeperData.Core.Messaging.Contracts.V1.Cts;
using MongoDB.Driver;

namespace KeeperData.Api.Tests.Integration.Orchestration.Cts;

[Trait("Dependence", "localstack")]
public class CtsImportHoldingMessageTests(IntegrationTestFixture fixture) : IClassFixture<IntegrationTestFixture>
{
    [Fact]
    public async Task GivenCtsImportHoldingMessage_WhenReceivedOnTheQueue_ShouldComplete()
    {
        var correlationId = Guid.NewGuid().ToString();
        var holdingIdentifier = Guid.NewGuid().ToString();
        var message = GetCtsImportHoldingMessage(holdingIdentifier);

        await ExecuteQueueTest(correlationId, message);

        // Wait briefly to allow processing
        await Task.Delay(TimeSpan.FromSeconds(5));

        var foundMessageProcesseEntryInLogs = await ContainerLoggingUtility.FindContainerLogEntryAsync(
            ContainerLoggingUtility.ServiceNameApi,
            $"Handled message with correlationId: \"{correlationId}\"");

        foundMessageProcesseEntryInLogs.Should().BeTrue();

        var ctsHoldingFilter = Builders<CtsHoldingDocument>.Filter.Eq(x => x.CountyParishHoldingNumber, holdingIdentifier);
        var ctsHoldings = await fixture.MongoVerifier.FindDocumentsAsync("ctsHoldings", ctsHoldingFilter);
        ctsHoldings.Should().NotBeNull().And.HaveCount(1);

        var ctsPartyFilter = Builders<CtsPartyDocument>.Filter.Eq(x => x.CountyParishHoldingNumber, holdingIdentifier);
        var ctsParties = await fixture.MongoVerifier.FindDocumentsAsync("ctsParties", ctsPartyFilter);
        ctsParties.Should().NotBeNull().And.HaveCount(2);

        var partyRoleRelationshipFilter = Builders<PartyRoleRelationshipDocument>.Filter.Eq(x => x.HoldingIdentifier, holdingIdentifier);
        var partyRoleRelationships = await fixture.MongoVerifier.FindDocumentsAsync("partyRoleRelationships", partyRoleRelationshipFilter);
        partyRoleRelationships.Should().NotBeNull().And.HaveCount(2);

        var partyIds = ctsParties.Select(x => x.PartyId).Distinct().ToHashSet();
        var partyRolePartyIds = partyRoleRelationships.Select(x => x.PartyId).Distinct().ToHashSet();
        partyIds.SetEquals(partyRolePartyIds).Should().BeTrue();
    }

    private async Task ExecuteQueueTest<TMessage>(string correlationId, TMessage message)
    {
        var queueUrl = "http://sqs.eu-west-2.127.0.0.1:4566/000000000000/ls_keeper_data_intake_queue";
        var additionalUserProperties = new Dictionary<string, string>
        {
            ["CorrelationId"] = correlationId
        };
        var request = SQSMessageUtility.CreateMessage(queueUrl, message, typeof(TMessage).Name, additionalUserProperties);

        using var cts = new CancellationTokenSource();
        await fixture.PublishToQueueAsync(request, cts.Token);
    }

    private static CtsImportHoldingMessage GetCtsImportHoldingMessage(string holdingIdentifier) => new()
    {
        Identifier = holdingIdentifier
    };
}