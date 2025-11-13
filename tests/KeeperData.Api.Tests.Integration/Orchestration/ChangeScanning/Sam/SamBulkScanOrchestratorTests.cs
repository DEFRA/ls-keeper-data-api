using FluentAssertions;
using KeeperData.Api.Tests.Integration.Consumers.Helpers;
using KeeperData.Api.Tests.Integration.Helpers;
using KeeperData.Core.Messaging.Contracts.V1.Sam;

namespace KeeperData.Api.Tests.Integration.Orchestration.ChangeScanning.Sam;

public class SamBulkScanOrchestratorTests(IntegrationTestFixture fixture) : IClassFixture<IntegrationTestFixture>
{
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

        await VerifySamHoldingImportPersistenceStepsCompleted(testExecutedOn, timeout, pollInterval, expectedEntries: LimitScanTotalBatchSize);
    }

    /// <summary>
    /// Looking for log entry:
    /// keeperdata_api | 2025-11-13T13:04:30.3440250+00:00 [INFO] (///KeeperData.Infrastructure.Messaging.Consumers.QueuePoller.) Handled message with correlationId: "d12cd3f8-0229-47a3-a9da-7d0e17005861"
    /// </summary>
    /// <param name="correlationId"></param>
    /// <param name="timeout"></param>
    /// <param name="pollInterval"></param>
    /// <returns></returns>
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

    /// <summary>
    /// Looking for log entries:
    /// keeperdata_api | 2025-11-13T13:04:30.3379762+00:00 [INFO] (///KeeperData.Application.Orchestration.Imports.Sam.Holdings.Steps.SamHoldingImportPersistenceStep.) Completed import step: "SamHoldingImportPersistenceStep" in 6ms
    /// </summary>
    /// <param name="testExecutedOn"></param>
    /// <param name="timeout"></param>
    /// <param name="pollInterval"></param>
    /// <param name="expectedEntries"></param>
    /// <returns></returns>
    private static async Task VerifySamHoldingImportPersistenceStepsCompleted(DateTime testExecutedOn, TimeSpan timeout, TimeSpan pollInterval, int expectedEntries)
    {
        var startTime = DateTime.UtcNow;
        var logFragment = "Completed import step: \"SamHoldingImportPersistenceStep\"";
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
        var queueUrl = "http://sqs.eu-west-2.127.0.0.1:4566/000000000000/ls_keeper_data_intake_queue";
        var additionalUserProperties = new Dictionary<string, string>
        {
            ["CorrelationId"] = correlationId
        };
        var request = SQSMessageUtility.CreateMessage(queueUrl, message, typeof(TMessage).Name, additionalUserProperties);

        using var cts = new CancellationTokenSource();
        await fixture.PublishToQueueAsync(request, cts.Token);
    }

    private static SamBulkScanMessage GetSamBulkScanMessage(string identifier) => new()
    {
        Identifier = identifier
    };
}