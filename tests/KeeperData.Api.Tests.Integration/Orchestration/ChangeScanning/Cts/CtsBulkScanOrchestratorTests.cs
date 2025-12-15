using FluentAssertions;
using KeeperData.Api.Tests.Integration.Consumers.Helpers;
using KeeperData.Api.Tests.Integration.Fixtures;
using KeeperData.Api.Tests.Integration.Helpers;
using KeeperData.Core.Messaging.Contracts.V1.Cts;

namespace KeeperData.Api.Tests.Integration.Orchestration.ChangeScanning.Cts;

[Trait("Dependence", "testcontainers")]
[Collection("Integration")]
public class CtsBulkScanOrchestratorTests(LocalStackFixture fixture)
{
    LocalStackFixture _localStackFixture = fixture;
    private const int ProcessingTimeCircuitBreakerSeconds = 30;
    private const int LimitScanTotalBatchSize = 10;

    /// <summary>
    /// Note. This requires the 'LimitScanTotalBatchSize' to match the 'DataBridgeScanConfiguration__LimitScanTotalBatchSize' from configuration.
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task GivenCtsBulkScanMessagePublishedToQueue_WhenReceivedOnTheQueue_ShouldScanForDocumentsAndComplete()
    {
        var correlationId = Guid.NewGuid().ToString();
        var identifier = Guid.NewGuid().ToString();
        var message = GetCtsBulkScanMessage(identifier);
        var testExecutedOn = DateTime.UtcNow;

        await ExecuteQueueTest(correlationId, message);

        var timeout = TimeSpan.FromSeconds(ProcessingTimeCircuitBreakerSeconds);
        var pollInterval = TimeSpan.FromSeconds(2);

        await VerifyCtsBulkScanMessageCompleted(correlationId, timeout, pollInterval);

        await VerifyCtsHoldingImportPersistenceStepsCompleted(correlationId, testExecutedOn, timeout, pollInterval, expectedEntries: LimitScanTotalBatchSize);
    }

    private static async Task VerifyCtsBulkScanMessageCompleted(string correlationId, TimeSpan timeout, TimeSpan pollInterval)
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

    private static async Task VerifyCtsHoldingImportPersistenceStepsCompleted(string correlationId, DateTime testExecutedOn, TimeSpan timeout, TimeSpan pollInterval, int expectedEntries)
    {
        var startTime = DateTime.UtcNow;
        var logFragment = $"Completed import step: \"CtsHoldingImportPersistenceStep\" correlationId: \"{correlationId}\"";
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

        matchingLogCount.Should().BeGreaterThanOrEqualTo(expectedEntries,
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

    private static CtsBulkScanMessage GetCtsBulkScanMessage(string identifier) => new()
    {
        Identifier = identifier
    };
}