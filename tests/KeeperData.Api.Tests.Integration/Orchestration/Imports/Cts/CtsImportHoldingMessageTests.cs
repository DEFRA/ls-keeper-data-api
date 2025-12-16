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

[Collection("Integration"), Trait("Dependence", "testcontainers")]
public class CtsImportHoldingMessageTests(
    MongoDbFixture mongoDbFixture,
    LocalStackFixture localStackFixture,
    ApiContainerFixture apiContainerFixture) : IAsyncLifetime
{
    private readonly MongoDbFixture _mongoDbFixture = mongoDbFixture;
    private readonly LocalStackFixture _localStackFixture = localStackFixture;
    private readonly ApiContainerFixture _apiContainerFixture = apiContainerFixture;

    private const int ProcessingTimeCircuitBreakerSeconds = 30;

    [Fact]
    public async Task GivenCtsImportHoldingMessage_WhenReceivedOnTheQueue_ShouldComplete()
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
                _apiContainerFixture.ApiContainer,
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
        var silverCtsHoldings = await _mongoDbFixture.MongoVerifier.FindDocumentsAsync("ctsHoldings", silverCtsHoldingFilter);
        silverCtsHoldings.Should().NotBeNull().And.HaveCount(1);

        var silverCtsPartyFilter = Builders<CtsPartyDocument>.Filter.Eq(x => x.CountyParishHoldingNumber, verifyHoldingIdentifier);
        var silverCtsParties = await _mongoDbFixture.MongoVerifier.FindDocumentsAsync("ctsParties", silverCtsPartyFilter);
        silverCtsParties.Should().NotBeNull().And.HaveCount(2);
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
        var request = SQSMessageUtility.CreateMessage(_localStackFixture.KrdsIntakeQueueUrl!, message, typeof(TMessage).Name, additionalUserProperties);

        using var cts = new CancellationTokenSource();
        await _localStackFixture.SqsClient.SendMessageAsync(request, cts.Token);
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
        await _mongoDbFixture.PurgeDataTables();
    }
}