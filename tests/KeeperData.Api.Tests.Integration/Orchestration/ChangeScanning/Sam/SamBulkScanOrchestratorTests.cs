using FluentAssertions;
using KeeperData.Api.Tests.Integration.Consumers.Helpers;
using KeeperData.Api.Tests.Integration.Fixtures;
using KeeperData.Api.Tests.Integration.Helpers;
using KeeperData.Core.Messaging.Contracts.V1.Sam;

namespace KeeperData.Api.Tests.Integration.Orchestration.ChangeScanning.Sam;

[Trait("Dependence", "testcontainers")]
[Collection("Integration")]
public class SamBulkScanOrchestratorTests(LocalStackFixture localStackFixture)
{
    private readonly LocalStackFixture _localStackFixture = localStackFixture;
    private const int ProcessingTimeCircuitBreakerSeconds = 30;
    private const int LimitScanTotalBatchSize = 10;

    /// <summary>
    /// Note. This requires the 'LimitScanTotalBatchSize' to match the 'DataBridgeScanConfiguration__LimitScanTotalBatchSize' from configuration.
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task GivenSamBulkScanMessagePublishedToQueue_WhenReceivedOnTheQueue_ShouldScanForDocumentsAndComplete()
    {
        var correlationId = Guid.NewGuid().ToString();
        var identifier = Guid.NewGuid().ToString();
        var message = GetSamBulkScanMessage(identifier);
        var testExecutedOn = DateTime.UtcNow;

        await ExecuteQueueTest(correlationId, message);

        var timeout = TimeSpan.FromSeconds(ProcessingTimeCircuitBreakerSeconds);
        var pollInterval = TimeSpan.FromSeconds(2);

        await VerifySamBulkScanMessageCompleted(correlationId, timeout, pollInterval);
        await VerifySamHoldingImportPersistenceStepsCompleted(correlationId, testExecutedOn, timeout, pollInterval, expectedEntries: LimitScanTotalBatchSize);
    }

    private static async Task VerifySamBulkScanMessageCompleted(string correlationId, TimeSpan timeout, TimeSpan pollInterval)
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

    private static async Task VerifySamHoldingImportPersistenceStepsCompleted(string correlationId, DateTime testExecutedOn, TimeSpan timeout, TimeSpan pollInterval, int expectedEntries)
    {
        var startTime = DateTime.UtcNow;
        var logFragment = $"Completed import step: \"SamHoldingImportPersistenceStep\" correlationId: \"{correlationId}\"";
        var matchingLogCount = 0;

        while (DateTime.UtcNow - startTime < timeout)
        {
            var logs = await ContainerLoggingUtility.FindContainerLogEntriesAsync(
                ContainerLoggingUtility.ServiceNameApi,
                logFragment);

            matchingLogCount = logs
                .Select(log =>
                {
                    var timestampToken = log.Split(' ').FirstOrDefault();
                    return DateTime.TryParse(timestampToken, out var timestamp) ? timestamp : (DateTime?)null;
                })
                .Where(ts => ts.HasValue && ts.Value >= testExecutedOn)
                .Count();

            if (matchingLogCount >= expectedEntries)
                break;

            await Task.Delay(pollInterval);
        }

        matchingLogCount.Should().Be(expectedEntries,
            $"Expected {expectedEntries} import step completions after {testExecutedOn:o} within {timeout.TotalSeconds} seconds.");
    }

    private async Task ExecuteQueueTest<TMessage>(string correlationId, TMessage message)
    {
        var additionalUserProperties = new Dictionary<string, string>
        {
            ["CorrelationId"] = correlationId
        };
        var request = SQSMessageUtility.CreateMessage(_localStackFixture.KrdsIntakeQueueUrl, message, typeof(TMessage).Name, additionalUserProperties);

        using var cts = new CancellationTokenSource();
        await _localStackFixture.SqsClient.SendMessageAsync(request, cts.Token);
    }

    private static SamBulkScanMessage GetSamBulkScanMessage(string identifier) => new()
    {
        Identifier = identifier
    };
}