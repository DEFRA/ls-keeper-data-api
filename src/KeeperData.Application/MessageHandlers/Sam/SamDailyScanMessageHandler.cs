using KeeperData.Application.Orchestration.ChangeScanning.Sam.Daily;
using KeeperData.Core.ApiClients.DataBridgeApi.Configuration;
using KeeperData.Core.Exceptions;
using KeeperData.Core.Messaging.Contracts;
using KeeperData.Core.Messaging.Contracts.V1.Sam;
using KeeperData.Core.Messaging.MessageHandlers;
using KeeperData.Core.Messaging.Serializers;

namespace KeeperData.Application.MessageHandlers.Sam;

public class SamDailyScanMessageHandler(SamDailyScanOrchestrator orchestrator,
  IUnwrappedMessageSerializer<SamDailyScanMessage> serializer,
  DataBridgeScanConfiguration dataBridgeScanConfiguration)
  : IMessageHandler<SamDailyScanMessage>
{
    private readonly IUnwrappedMessageSerializer<SamDailyScanMessage> _serializer = serializer;
    private readonly SamDailyScanOrchestrator _orchestrator = orchestrator;
    private readonly DataBridgeScanConfiguration _dataBridgeScanConfiguration = dataBridgeScanConfiguration;

    public async Task<MessageType> Handle(UnwrappedMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message, nameof(message));

        var messagePayload = _serializer.Deserialize(message)
            ?? throw new NonRetryableException($"Deserialisation failed or the message payload was null for " +
            $"messageType: {typeof(SamDailyScanMessage).Name}," +
            $"messageId: {message.MessageId}," +
            $"correlationId: {message.CorrelationId}");

        var context = new SamDailyScanContext
        {
            CurrentDateTime = DateTime.UtcNow,
            UpdatedSinceDateTime = DateTime.UtcNow.AddHours(-24),
            PageSize = _dataBridgeScanConfiguration.QueryPageSize,
            Holdings = new(),
            Holders = new(),
            Herds = new(),
            Parties = new()
        };

        await _orchestrator.ExecuteAsync(context, cancellationToken);

        return await Task.FromResult(messagePayload!);
    }
}