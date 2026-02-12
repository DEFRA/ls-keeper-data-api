using KeeperData.Application.Commands;
using KeeperData.Application.Commands.MessageProcessing;
using KeeperData.Application.Orchestration.ChangeScanning.Cts.Bulk;
using KeeperData.Core.ApiClients.DataBridgeApi.Configuration;
using KeeperData.Core.Exceptions;
using KeeperData.Core.Messaging.Contracts;
using KeeperData.Core.Messaging.Contracts.V1.Cts;
using KeeperData.Core.Messaging.Serializers;
using KeeperData.Core.Telemetry;
using System.Diagnostics;

namespace KeeperData.Application.MessageHandlers.Cts;

public class CtsBulkScanMessageHandler(CtsBulkScanOrchestrator orchestrator,
  IUnwrappedMessageSerializer<CtsBulkScanMessage> serializer,
  DataBridgeScanConfiguration dataBridgeScanConfiguration,
  IApplicationMetrics metrics)
  : ICommandHandler<ProcessCtsBulkScanMessageCommand, MessageType>
{
    private readonly IUnwrappedMessageSerializer<CtsBulkScanMessage> _serializer = serializer;
    private readonly CtsBulkScanOrchestrator _orchestrator = orchestrator;
    private readonly DataBridgeScanConfiguration _dataBridgeScanConfiguration = dataBridgeScanConfiguration;
    private readonly IApplicationMetrics _metrics = metrics;

    public async Task<MessageType> Handle(ProcessCtsBulkScanMessageCommand request, CancellationToken cancellationToken)
    {
        var processingStopwatch = Stopwatch.StartNew();
        var message = request.Message;

        ArgumentNullException.ThrowIfNull(message);

        _metrics.RecordCount(MetricNames.Queue, 1,
            (MetricNames.CommonTags.Operation, MetricNames.Operations.BatchStarted),
            (MetricNames.CommonTags.UpdateType, "cts_bulk_scan"));

        try
        {
            var messagePayload = _serializer.Deserialize(message)
                ?? throw new NonRetryableException($"Deserialisation failed or the message payload was null for " +
                $"messageType: {typeof(CtsBulkScanMessage).Name}," +
                $"messageId: {message.MessageId}," +
                $"correlationId: {message.CorrelationId}");

            var context = new CtsBulkScanContext
            {
                CurrentDateTime = DateTime.UtcNow,
                UpdatedSinceDateTime = null,
                PageSize = _dataBridgeScanConfiguration.QueryPageSize,
                Holdings = new()
            };

            await _orchestrator.ExecuteAsync(context, cancellationToken);

            processingStopwatch.Stop();
            
            _metrics.RecordValue(MetricNames.Queue, processingStopwatch.ElapsedMilliseconds,
                (MetricNames.CommonTags.Operation, MetricNames.Operations.BatchDuration));
                
            _metrics.RecordCount(MetricNames.Queue, 1,
                (MetricNames.CommonTags.Operation, MetricNames.Operations.BatchProcessed));

            return await Task.FromResult(messagePayload!);
        }
        catch (Exception ex)
        {
            processingStopwatch.Stop();
            
            _metrics.RecordCount(MetricNames.Queue, 1,
                (MetricNames.CommonTags.Operation, MetricNames.Operations.BatchFailed),
                (MetricNames.CommonTags.ErrorType, ex.GetType().Name));
                
            throw;
        }
    }
}