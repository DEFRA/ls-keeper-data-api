using FluentAssertions;
using KeeperData.Api.Tests.Integration.Consumers.Helpers;
using KeeperData.Api.Tests.Integration.Helpers;
using KeeperData.Core.Documents;
using KeeperData.Core.Documents.Silver;
using KeeperData.Core.Domain.Enums;
using KeeperData.Core.Messaging.Contracts.V1.Sam;
using KeeperData.Tests.Common.Generators;
using MongoDB.Driver;

namespace KeeperData.Api.Tests.Integration.Orchestration.Sam;

[Trait("Dependence", "localstack")]
public class SamImportHoldingMessageTests(IntegrationTestFixture fixture) : IClassFixture<IntegrationTestFixture>
{
    [Fact]
    public async Task GivenSamImportHoldingMessage_WhenReceivedOnTheQueue_ShouldComplete()
    {
        var correlationId = Guid.NewGuid().ToString();
        var holdingIdentifier = CphGenerator.GenerateFormattedCph();
        var message = GetSamImportHoldingMessage(holdingIdentifier);

        await ExecuteQueueTest(correlationId, message);

        // Wait briefly to allow processing
        await Task.Delay(TimeSpan.FromSeconds(5));

        var foundMessageProcesseEntryInLogs = await ContainerLoggingUtility.FindContainerLogEntryAsync(
            ContainerLoggingUtility.ServiceNameApi,
            $"Handled message with correlationId: \"{correlationId}\"");

        foundMessageProcesseEntryInLogs.Should().BeTrue();

        await VerifySilverDataTypesAsync(holdingIdentifier);
        await VerifyGoldDataTypesAsync(holdingIdentifier);
    }

    private async Task VerifySilverDataTypesAsync(string holdingIdentifier)
    {
        var silverSamHoldingFilter = Builders<SamHoldingDocument>.Filter.Eq(x => x.CountyParishHoldingNumber, holdingIdentifier);
        var silverSamHoldings = await fixture.MongoVerifier.FindDocumentsAsync("samHoldings", silverSamHoldingFilter);

        var partyRoleRelationshipFilter = Builders<Core.Documents.Silver.SitePartyRoleRelationshipDocument>.Filter.Eq(x => x.HoldingIdentifier, holdingIdentifier);
        var partyRoleRelationships = await fixture.MongoVerifier.FindDocumentsAsync("silverSitePartyRoleRelationships", partyRoleRelationshipFilter);
        var partyRolePartyIds = partyRoleRelationships.Select(r => r.PartyId).Distinct().ToList();

        var silverSamPartyFilter = Builders<SamPartyDocument>.Filter.In(x => x.PartyId, partyRolePartyIds);
        var silverSamParties = await fixture.MongoVerifier.FindDocumentsAsync("samParties", silverSamPartyFilter);
        var partyIds = silverSamParties.Select(x => x.PartyId).Distinct().ToHashSet();

        silverSamHoldings.Should().NotBeNull().And.HaveCount(1);
        silverSamParties.Should().NotBeNull().And.HaveCountGreaterThanOrEqualTo(1);
        partyRoleRelationships.Should().NotBeNull().And.HaveCount(silverSamParties.Count);
        partyIds.SetEquals(partyRolePartyIds).Should().BeTrue();

        var silverSamHerdFilter = Builders<SamHerdDocument>.Filter.Eq(x => x.CountyParishHoldingHerd, holdingIdentifier);
        var silverSamHerds = await fixture.MongoVerifier.FindDocumentsAsync("samHerds", silverSamHerdFilter);
        silverSamHerds.Should().NotBeNull().And.HaveCount(1);
    }

    private async Task VerifyGoldDataTypesAsync(string holdingIdentifier)
    {
        var holdingIdentifierType = HoldingIdentifierType.CphNumber.ToString();

        var siteFilter = Builders<SiteDocument>.Filter.ElemMatch(
            x => x.Identifiers,
            i => i.Identifier == holdingIdentifier && i.Type == holdingIdentifierType);
        var sites = await fixture.MongoVerifier.FindDocumentsAsync("sites", siteFilter);
        sites.Should().NotBeNull().And.HaveCount(1);

        // TODO - Add additional records
    }

    private async Task ExecuteQueueTest<TMessage>(string correlationId, TMessage message)
    {
        var queueUrl = "http://sqs.eu-west-2.127.0.0.1:4566/000000000000/ls_keeper_data_intake_queue";
        var additionalUserProperties = new Dictionary<string, string>
        {
            ["CorrelationId"] = correlationId
        };
        var request = SQSMessageUtility.CreateMessage(queueUrl, message, typeof(TMessage).Name, additionalUserProperties);

        using var sam = new CancellationTokenSource();
        await fixture.PublishToQueueAsync(request, sam.Token);
    }

    private static SamImportHoldingMessage GetSamImportHoldingMessage(string holdingIdentifier) => new()
    {
        Identifier = holdingIdentifier
    };
}