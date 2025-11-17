using KeeperData.Application.Orchestration.ChangeScanning.Sam.Bulk;
using KeeperData.Core.ApiClients.DataBridgeApi.Configuration;
using KeeperData.Core.Exceptions;
using KeeperData.Core.Messaging.Contracts;
using KeeperData.Core.Messaging.Contracts.V1.Sam;
using KeeperData.Core.Messaging.MessageHandlers;
using KeeperData.Core.Messaging.Serializers;

namespace KeeperData.Application.MessageHandlers.Sam;

public class SamBulkScanMessageHandler(SamBulkScanOrchestrator orchestrator,
  IUnwrappedMessageSerializer<SamBulkScanMessage> serializer,
  DataBridgeScanConfiguration dataBridgeScanConfiguration)
  : IMessageHandler<SamBulkScanMessage>
{
    private readonly IUnwrappedMessageSerializer<SamBulkScanMessage> _serializer = serializer;
    private readonly SamBulkScanOrchestrator _orchestrator = orchestrator;
    private readonly DataBridgeScanConfiguration _dataBridgeScanConfiguration = dataBridgeScanConfiguration;

    public async Task<MessageType> Handle(UnwrappedMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message, nameof(message));

        var messagePayload = _serializer.Deserialize(message)
            ?? throw new NonRetryableException($"Deserialisation failed or the message payload was null for " +
            $"messageType: {typeof(SamBulkScanMessage).Name}," +
            $"messageId: {message.MessageId}," +
            $"correlationId: {message.CorrelationId}");

        var context = new SamBulkScanContext
        {
            CurrentDateTime = DateTime.UtcNow,
            UpdatedSinceDateTime = null,
            PageSize = _dataBridgeScanConfiguration.QueryPageSize,
            Holdings = new(),
            Holders = new()
        };

        await _orchestrator.ExecuteAsync(context, cancellationToken);

        return await Task.FromResult(messagePayload!);
    }
}