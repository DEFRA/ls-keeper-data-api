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
[Collection("Integration Tests")]
public class CtsImportHoldingMessageTests(IntegrationTestFixture fixture) : IClassFixture<IntegrationTestFixture>
{
    private const int ProcessingTimeCircuitBreakerSeconds = 30;

    [Fact]
    public async Task GivenCtsImportHoldingMessage_WhenReceivedOnTheQueue_ShouldComplete()
    {
        var correlationId = Guid.NewGuid().ToString();
        var holdingIdentifier = CphGenerator.GenerateCtsFormattedLidIdentifier("AH");
        var message = GetCtsImportHoldingMessage(holdingIdentifier);
        var testExecutedOn = DateTime.UtcNow;

        await ExecuteQueueTest(correlationId, message);

        var timeout = TimeSpan.FromSeconds(ProcessingTimeCircuitBreakerSeconds);
        var pollInterval = TimeSpan.FromSeconds(2);

        await VerifyCtsImportHoldingMessageCompleted(correlationId, timeout, pollInterval);

        await VerifySilverDataTypesAsync(holdingIdentifier);

        await VerifyGoldDataTypesAsync(holdingIdentifier);
    }

    private static async Task VerifyCtsImportHoldingMessageCompleted(string correlationId, TimeSpan timeout, TimeSpan pollInterval)
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
    }

    private Task VerifyGoldDataTypesAsync(string holdingIdentifier)
    {
        // TODO - Add Gold

        return Task.CompletedTask;
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