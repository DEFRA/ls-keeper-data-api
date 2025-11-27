using FluentAssertions;
using KeeperData.Api.Tests.Integration.Consumers.Helpers;
using KeeperData.Api.Tests.Integration.Helpers;
using KeeperData.Core.Documents.Silver;
using KeeperData.Core.Messaging.Contracts.V1.Sam;
using MongoDB.Driver;

namespace KeeperData.Api.Tests.Integration.Orchestration.Imports.Sam;

[Trait("Dependence", "localstack")]
[Collection("Integration Tests")]
public class SamImportHoldersMessageTests(IntegrationTestFixture fixture) : IClassFixture<IntegrationTestFixture>
{
    private const int ProcessingTimeCircuitBreakerSeconds = 30;

    [Fact]
    public async Task GivenSamImportHolderMessage_WhenReceivedOnTheQueue_ShouldComplete()
    {
        var correlationId = Guid.NewGuid().ToString();
        var partyId = Guid.NewGuid().ToString();
        var message = GetSamImportHolderMessage(partyId);
        var testExecutedOn = DateTime.UtcNow;

        await ExecuteQueueTest(correlationId, message);

        var timeout = TimeSpan.FromSeconds(ProcessingTimeCircuitBreakerSeconds);
        var pollInterval = TimeSpan.FromSeconds(2);

        await VerifySamImportHolderMessageCompleted(correlationId, timeout, pollInterval);

        await VerifySilverDataTypesAsync(partyId);

        await VerifyGoldDataTypesAsync(partyId);
    }

    private static async Task VerifySamImportHolderMessageCompleted(string correlationId, TimeSpan timeout, TimeSpan pollInterval)
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

    private async Task VerifySilverDataTypesAsync(string partyId)
    {
        var partyRoleRelationshipFilter = Builders<SitePartyRoleRelationshipDocument>.Filter.Eq(x => x.PartyId, partyId);
        var partyRoleRelationships = await fixture.MongoVerifier.FindDocumentsAsync("silverSitePartyRoleRelationships", partyRoleRelationshipFilter);
        var partyRolePartyIds = partyRoleRelationships.Select(r => r.PartyId).Distinct().ToList();

        var silverSamPartyFilter = Builders<SamPartyDocument>.Filter.In(x => x.PartyId, partyRolePartyIds);
        var silverSamParties = await fixture.MongoVerifier.FindDocumentsAsync("samParties", silverSamPartyFilter);
        var partyIds = silverSamParties.Select(x => x.PartyId).Distinct().ToHashSet();

        silverSamParties.Should().NotBeNull().And.HaveCountGreaterThanOrEqualTo(1);
        partyRoleRelationships.Should().NotBeNull().And.HaveCount(silverSamParties.Count);
        partyIds.SetEquals(partyRolePartyIds).Should().BeTrue();
    }

    private Task VerifyGoldDataTypesAsync(string partyId)
    {
        // TODO - Add additional records

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

        using var sam = new CancellationTokenSource();
        await fixture.PublishToQueueAsync(request, sam.Token);
    }

    private static SamImportHolderMessage GetSamImportHolderMessage(string partyId) => new()
    {
        Identifier = partyId
    };
}