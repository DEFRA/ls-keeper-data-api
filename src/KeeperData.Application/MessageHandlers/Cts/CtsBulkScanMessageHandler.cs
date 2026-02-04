using KeeperData.Application.Commands;
using KeeperData.Application.Commands.MessageProcessing;
using KeeperData.Application.Orchestration.ChangeScanning.Cts.Bulk;
using KeeperData.Core.ApiClients.DataBridgeApi.Configuration;
using KeeperData.Core.Exceptions;
using KeeperData.Core.Messaging.Contracts;
using KeeperData.Core.Messaging.Contracts.V1.Cts;
using KeeperData.Core.Messaging.Serializers;

namespace KeeperData.Application.MessageHandlers.Cts;

public class CtsBulkScanMessageHandler(CtsBulkScanOrchestrator orchestrator,
  IUnwrappedMessageSerializer<CtsBulkScanMessage> serializer,
  DataBridgeScanConfiguration dataBridgeScanConfiguration)
  : ICommandHandler<ProcessCtsBulkScanMessageCommand, MessageType>
{
    private readonly IUnwrappedMessageSerializer<CtsBulkScanMessage> _serializer = serializer;
    private readonly CtsBulkScanOrchestrator _orchestrator = orchestrator;
    private readonly DataBridgeScanConfiguration _dataBridgeScanConfiguration = dataBridgeScanConfiguration;

    public async Task<MessageType> Handle(ProcessCtsBulkScanMessageCommand request, CancellationToken cancellationToken)
    {
        var message = request.Message;

        ArgumentNullException.ThrowIfNull(message, nameof(message));

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

        return await Task.FromResult(messagePayload!);
    }
}