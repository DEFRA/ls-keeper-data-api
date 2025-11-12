using FluentAssertions;
using KeeperData.Api.Tests.Integration.Consumers.Helpers;
using KeeperData.Api.Tests.Integration.Helpers;
using KeeperData.Core.Documents.Silver;
using KeeperData.Core.Domain.Sites.Formatters;
using KeeperData.Core.Messaging.Contracts.V1.Cts;
using KeeperData.Tests.Common.Generators;
using MongoDB.Driver;

namespace KeeperData.Api.Tests.Integration.Orchestration.Imports.Cts;

[Trait("Dependence", "localstack")]
public class CtsImportHoldingMessageTests(IntegrationTestFixture fixture) : IClassFixture<IntegrationTestFixture>
{
    [Fact]
    public async Task GivenCtsImportHoldingMessage_WhenReceivedOnTheQueue_ShouldComplete()
    {
        var correlationId = "AG-3000158"; // Guid.NewGuid().ToString();
        var holdingIdentifier = CphGenerator.GenerateCtsFormattedLidIdentifier("AH");
        var message = GetCtsImportHoldingMessage(holdingIdentifier);

        await ExecuteQueueTest(correlationId, message);

        // Wait briefly to allow processing
        await Task.Delay(TimeSpan.FromSeconds(5));

        var foundMessageProcesseEntryInLogs = await ContainerLoggingUtility.FindContainerLogEntryAsync(
            ContainerLoggingUtility.ServiceNameApi,
            $"Handled message with correlationId: \"{correlationId}\"");

        foundMessageProcesseEntryInLogs.Should().BeTrue();

        var verifyHoldingIdentifier = holdingIdentifier.LidIdentifierToCph();

        var silverCtsHoldingFilter = Builders<CtsHoldingDocument>.Filter.Eq(x => x.CountyParishHoldingNumber, verifyHoldingIdentifier);
        var silverCtsHoldings = await fixture.MongoVerifier.FindDocumentsAsync("ctsHoldings", silverCtsHoldingFilter);
        silverCtsHoldings.Should().NotBeNull().And.HaveCount(1);

        var silverCtsPartyFilter = Builders<CtsPartyDocument>.Filter.Eq(x => x.CountyParishHoldingNumber, verifyHoldingIdentifier);
        var silverCtsParties = await fixture.MongoVerifier.FindDocumentsAsync("ctsParties", silverCtsPartyFilter);
        silverCtsParties.Should().NotBeNull().And.HaveCount(2);

        var partyRoleRelationshipFilter = Builders<SitePartyRoleRelationshipDocument>.Filter.Eq(x => x.HoldingIdentifier, verifyHoldingIdentifier);
        var partyRoleRelationships = await fixture.MongoVerifier.FindDocumentsAsync("silverSitePartyRoleRelationships", partyRoleRelationshipFilter);
        partyRoleRelationships.Should().NotBeNull().And.HaveCount(2);

        var partyIds = silverCtsParties.Select(x => x.PartyId).Distinct().ToHashSet();
        var partyRolePartyIds = partyRoleRelationships.Select(x => x.PartyId).Distinct().ToHashSet();
        partyIds.SetEquals(partyRolePartyIds).Should().BeTrue();

        // TODO - Add Gold
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