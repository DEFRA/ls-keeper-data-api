using FluentAssertions;
using KeeperData.Api.Tests.Integration.Consumers.Helpers;
using KeeperData.Api.Tests.Integration.Fixtures;
using KeeperData.Api.Tests.Integration.Helpers;
using KeeperData.Core.Documents.Silver;
using KeeperData.Core.Domain.Sites.Formatters;
using KeeperData.Core.Messaging.Contracts.V1.Cts;
using KeeperData.Tests.Common.Generators;
using MongoDB.Driver;

namespace KeeperData.Api.Tests.Integration.Orchestration.Imports.Cts;

[Collection("IntegrationAnonymization"), Trait("Dependence", "testcontainers")]
public class CtsImportHoldingAnonMessageTests(
    MongoDbAnonymousFixture mongoDbFixture,
    LocalStackAnonymousFixture localStackFixture,
    ApiAnonymousContainerFixture apiAnonymousContainerFixture) : IAsyncLifetime
{
    private const int ProcessingTimeCircuitBreakerSeconds = 30;

    [Fact]
    public async Task GivenCtsImportHoldingMessage_WhenReceivedOnTheQueue_ShouldCompleteWithAnonymizedData()
    {
        var correlationId = Guid.NewGuid().ToString();
        var holdingIdentifier = CphGenerator.GenerateCtsFormattedLidIdentifier("AH");
        var message = GetCtsImportHoldingMessage(holdingIdentifier);

        await ExecuteQueueTest(correlationId, message);

        var timeout = TimeSpan.FromSeconds(ProcessingTimeCircuitBreakerSeconds);
        var pollInterval = TimeSpan.FromSeconds(2);

        await VerifyCtsImportHoldingMessageCompleted(correlationId, timeout, pollInterval);

        await VerifySilverDataTypesAsync(holdingIdentifier);

        await VerifyGoldDataTypesAsync();
    }

    private async Task VerifyCtsImportHoldingMessageCompleted(string correlationId, TimeSpan timeout, TimeSpan pollInterval)
    {
        var startTime = DateTime.UtcNow;
        var foundLogEntry = false;

        while (DateTime.UtcNow - startTime < timeout)
        {
            foundLogEntry = await ContainerLoggingUtility.FindContainerLogEntryAsync(
                apiAnonymousContainerFixture.ApiContainer,
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
        var silverCtsHoldings = await mongoDbFixture.MongoVerifier.FindDocumentsAsync("ctsHoldings", silverCtsHoldingFilter);
        silverCtsHoldings.Should().NotBeNull().And.HaveCount(1);

        var holding = silverCtsHoldings.First();

        // Verify anonymization: LocationName should not be a GUID (fake data should be human-readable)
        holding.LocationName.Should().NotBeNullOrEmpty();
        Guid.TryParse(holding.LocationName, out _).Should().BeFalse(
            "LocationName should be anonymized with fake data, not a GUID");

        var silverCtsPartyFilter = Builders<CtsPartyDocument>.Filter.Eq(x => x.CountyParishHoldingNumber, verifyHoldingIdentifier);
        var silverCtsParties = await mongoDbFixture.MongoVerifier.FindDocumentsAsync("ctsParties", silverCtsPartyFilter);
        silverCtsParties.Should().NotBeNull().And.HaveCount(2);

        // Verify anonymization: PartyLastName should not be a GUID (fake data should be human-readable)
        foreach (var party in silverCtsParties)
        {
            party.PartyLastName.Should().NotBeNullOrEmpty();
            Guid.TryParse(party.PartyLastName, out _).Should().BeFalse(
                $"PartyLastName for party {party.PartyId} should be anonymized with fake data, not a GUID");
        }
    }

    private static Task VerifyGoldDataTypesAsync()
    {
        return Task.CompletedTask;
    }

    private async Task ExecuteQueueTest<TMessage>(string correlationId, TMessage message)
    {
        var additionalUserProperties = new Dictionary<string, string>
        {
            ["CorrelationId"] = correlationId
        };
        var request = SQSMessageUtility.CreateMessage(localStackFixture.KrdsIntakeQueueUrl!, message, typeof(TMessage).Name, additionalUserProperties);

        using var cts = new CancellationTokenSource();
        await localStackFixture.SqsClient.SendMessageAsync(request, cts.Token);
    }

    private static CtsImportHoldingMessage GetCtsImportHoldingMessage(string holdingIdentifier) => new()
    {
        Identifier = holdingIdentifier
    };

    public async Task InitializeAsync()
    {
        await Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await mongoDbFixture.PurgeDataTables();
    }
}