using FluentAssertions;
using KeeperData.Api.Tests.Integration.Consumers.Helpers;
using KeeperData.Api.Tests.Integration.Helpers;
using KeeperData.Core.Documents;
using KeeperData.Core.Documents.Silver;
using KeeperData.Core.Domain.Enums;
using KeeperData.Core.Messaging.Contracts.V1.Sam;
using KeeperData.Tests.Common.Generators;
using MongoDB.Driver;

namespace KeeperData.Api.Tests.Integration.Orchestration.Imports.Sam;

[Collection("Integration"), Trait("Dependence", "testcontainers")] // TODO affects data
public class SamImportHoldingMessageTests(MongoDbFixture mongoDbFixture, LocalStackFixture localStackFixture, ApiContainerFixture apiContainerFixture) : IAsyncLifetime
{
    private readonly MongoDbFixture _mongoDbFixture = mongoDbFixture;
    private readonly LocalStackFixture _localStackFixture = localStackFixture;
    private readonly ApiContainerFixture _apiContainerFixture = apiContainerFixture;
    private const int ProcessingTimeCircuitBreakerSeconds = 30;

    [Fact]
    public async Task GivenSamImportHoldingMessage_WhenReceivedOnTheQueue_ShouldComplete()
    {
        var correlationId = Guid.NewGuid().ToString();
        var holdingIdentifier = CphGenerator.GenerateFormattedCph();
        var message = GetSamImportHoldingMessage(holdingIdentifier);
        var testExecutedOn = DateTime.UtcNow;

        await ExecuteQueueTest(correlationId, message);

        var timeout = TimeSpan.FromSeconds(ProcessingTimeCircuitBreakerSeconds);
        var pollInterval = TimeSpan.FromSeconds(2);

        await VerifySamImportHoldingMessageCompleted(correlationId, timeout, pollInterval);

        await VerifySilverDataTypesAsync(holdingIdentifier);

        await VerifyGoldDataTypesAsync(holdingIdentifier);
    }

    private static async Task VerifySamImportHoldingMessageCompleted(string correlationId, TimeSpan timeout, TimeSpan pollInterval)
    {
        var startTime = DateTime.UtcNow;
        var foundLogEntry = false;

        while (DateTime.UtcNow - startTime < timeout)
        {
            foundLogEntry = await ContainerLoggingUtility.FindContainerLogEntryAsync(
                ContainerLoggingUtility.ServiceNameApi,
                $"Handled message with correlationId: \"{correlationId}\"");

            if (foundLogEntry)
                break;

            await Task.Delay(pollInterval);
        }

        foundLogEntry.Should().BeTrue($"Expected log entry within {ProcessingTimeCircuitBreakerSeconds} seconds but none was found.");
    }

    private async Task VerifySilverDataTypesAsync(string holdingIdentifier)
    {
        var silverSamHoldingFilter = Builders<SamHoldingDocument>.Filter.Eq(x => x.CountyParishHoldingNumber, holdingIdentifier);
        var silverSamHoldings = await _mongoDbFixture.MongoVerifier.FindDocumentsAsync("samHoldings", silverSamHoldingFilter);

        var partyRoleRelationshipFilter = Builders<Core.Documents.Silver.SitePartyRoleRelationshipDocument>.Filter.Eq(x => x.HoldingIdentifier, holdingIdentifier);
        var partyRoleRelationships = await _mongoDbFixture.MongoVerifier.FindDocumentsAsync("silverSitePartyRoleRelationships", partyRoleRelationshipFilter);
        var partyRolePartyIds = partyRoleRelationships.Select(r => r.PartyId).Distinct().ToList();

        var silverSamPartyFilter = Builders<SamPartyDocument>.Filter.In(x => x.PartyId, partyRolePartyIds);
        var silverSamParties = await _mongoDbFixture.MongoVerifier.FindDocumentsAsync("samParties", silverSamPartyFilter);
        var partyIds = silverSamParties.Select(x => x.PartyId).Distinct().ToHashSet();

        silverSamHoldings.Should().NotBeNull().And.HaveCount(1);
        silverSamParties.Should().NotBeNull().And.HaveCountGreaterThanOrEqualTo(1);
        partyRoleRelationships.Should().NotBeNull().And.HaveCount(silverSamParties.Count);
        partyIds.SetEquals(partyRolePartyIds).Should().BeTrue();

        var silverSamHerdFilter = Builders<SamHerdDocument>.Filter.Eq(x => x.CountyParishHoldingHerd, $"{holdingIdentifier}/01");
        var silverSamHerds = await _mongoDbFixture.MongoVerifier.FindDocumentsAsync("samHerds", silverSamHerdFilter);
        silverSamHerds.Should().NotBeNull().And.HaveCount(1);
    }

    private async Task VerifyGoldDataTypesAsync(string holdingIdentifier)
    {
        var holdingIdentifierType = HoldingIdentifierType.CphNumber.ToString();

        var siteFilter = Builders<SiteDocument>.Filter.ElemMatch(
            x => x.Identifiers,
            i => i.Identifier == holdingIdentifier && i.Type == holdingIdentifierType);
        var sites = await _mongoDbFixture.MongoVerifier.FindDocumentsAsync("sites", siteFilter);
        sites.Should().NotBeNull().And.HaveCount(1);
    }

    private async Task ExecuteQueueTest<TMessage>(string correlationId, TMessage message)
    {
        var additionalUserProperties = new Dictionary<string, string>
        {
            ["CorrelationId"] = correlationId
        };
        var request = SQSMessageUtility.CreateMessage(_localStackFixture.LsKeeperDataIntakeQueue, message, typeof(TMessage).Name, additionalUserProperties);

        using var sam = new CancellationTokenSource();
        await _localStackFixture.SqsClient.SendMessageAsync(request, sam.Token);
    }

    private static SamImportHoldingMessage GetSamImportHoldingMessage(string holdingIdentifier) => new()
    {
        Identifier = holdingIdentifier
    };

    public async Task InitializeAsync()
    {
        await Task.CompletedTask;
    }

    public async Task DisposeAsync()
    { // TODO checkme
        await _mongoDbFixture.MongoVerifier.DeleteAll<PartyDocument>();
        await _mongoDbFixture.MongoVerifier.DeleteAll<SiteDocument>();
        await _mongoDbFixture.MongoVerifier.DeleteAll<SamPartyDocument>();
        await _mongoDbFixture.MongoVerifier.DeleteAll<SamHerdDocument>();
        await _mongoDbFixture.MongoVerifier.DeleteAll<SamHoldingDocument>();
        await _mongoDbFixture.MongoVerifier.DeleteAll<Core.Documents.Silver.SitePartyRoleRelationshipDocument>();
        await _mongoDbFixture.MongoVerifier.DeleteAll<CtsHoldingDocument>();
    }
}