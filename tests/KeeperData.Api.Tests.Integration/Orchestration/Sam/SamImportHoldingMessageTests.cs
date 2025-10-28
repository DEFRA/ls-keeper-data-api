using FluentAssertions;
using KeeperData.Api.Tests.Integration.Consumers.Helpers;
using KeeperData.Api.Tests.Integration.Helpers;
using KeeperData.Core.Documents.Silver;
using KeeperData.Core.Messaging.Contracts.V1.Sam;
using MongoDB.Driver;

namespace KeeperData.Api.Tests.Integration.Orchestration.Sam;

[Trait("Dependence", "localstack")]
public class SamImportHoldingMessageTests(IntegrationTestFixture fixture) : IClassFixture<IntegrationTestFixture>
{
    [Fact]
    public async Task GivenSamImportHoldingMessage_WhenReceivedOnTheQueue_ShouldComplete()
    {
        var correlationId = Guid.NewGuid().ToString();
        var holdingIdentifier = Guid.NewGuid().ToString();
        var message = GetSamImportHoldingMessage(holdingIdentifier);

        await ExecuteQueueTest(correlationId, message);

        // Wait briefly to allow processing
        await Task.Delay(TimeSpan.FromSeconds(5));

        var foundMessageProcesseEntryInLogs = await ContainerLoggingUtility.FindContainerLogEntryAsync(
            ContainerLoggingUtility.ServiceNameApi,
            $"Handled message with correlationId: \"{correlationId}\"");

        foundMessageProcesseEntryInLogs.Should().BeTrue();

        var silverSamHoldingFilter = Builders<SamHoldingDocument>.Filter.Eq(x => x.CountyParishHoldingNumber, holdingIdentifier);
        var silverSamHoldings = await fixture.MongoVerifier.FindDocumentsAsync("samHoldings", silverSamHoldingFilter);
        silverSamHoldings.Should().NotBeNull().And.HaveCount(1);

        var silverSamPartyFilter = Builders<SamPartyDocument>.Filter.Eq(x => x.CountyParishHoldingNumber, holdingIdentifier);
        var silverSamParties = await fixture.MongoVerifier.FindDocumentsAsync("samParties", silverSamPartyFilter);
        silverSamParties.Should().NotBeNull().And.HaveCountGreaterThanOrEqualTo(1);

        var partyRoleRelationshipFilter = Builders<PartyRoleRelationshipDocument>.Filter.Eq(x => x.HoldingIdentifier, holdingIdentifier);
        var partyRoleRelationships = await fixture.MongoVerifier.FindDocumentsAsync("partyRoleRelationships", partyRoleRelationshipFilter);
        partyRoleRelationships.Should().NotBeNull().And.HaveCount(silverSamParties.Count);

        var partyIds = silverSamParties.Select(x => x.PartyId).Distinct().ToHashSet();
        var partyRolePartyIds = partyRoleRelationships.Select(x => x.PartyId).Distinct().ToHashSet();
        partyIds.SetEquals(partyRolePartyIds).Should().BeTrue();

        var silverSamHerdFilter = Builders<SamHerdDocument>.Filter.Eq(x => x.CountyParishHoldingHerd, holdingIdentifier);
        var silverSamHerds = await fixture.MongoVerifier.FindDocumentsAsync("samHerds", silverSamHerdFilter);
        silverSamHerds.Should().NotBeNull().And.HaveCount(1);

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

        using var sam = new CancellationTokenSource();
        await fixture.PublishToQueueAsync(request, sam.Token);
    }

    private static SamImportHoldingMessage GetSamImportHoldingMessage(string holdingIdentifier) => new()
    {
        Identifier = holdingIdentifier
    };
}