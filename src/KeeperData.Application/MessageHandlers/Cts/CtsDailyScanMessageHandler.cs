using KeeperData.Application.Commands;
using KeeperData.Application.Commands.MessageProcessing;
using KeeperData.Application.Orchestration.ChangeScanning.Cts.Daily;
using KeeperData.Core.ApiClients.DataBridgeApi.Configuration;
using KeeperData.Core.Exceptions;
using KeeperData.Core.Messaging.Contracts;
using KeeperData.Core.Messaging.Contracts.V1.Cts;
using KeeperData.Core.Messaging.Serializers;

namespace KeeperData.Application.MessageHandlers.Cts;

public class CtsDailyScanMessageHandler(CtsDailyScanOrchestrator orchestrator,
  IUnwrappedMessageSerializer<CtsDailyScanMessage> serializer,
  DataBridgeScanConfiguration dataBridgeScanConfiguration)
  : ICommandHandler<ProcessCtsDailyScanMessageCommand, MessageType>
{
    private readonly IUnwrappedMessageSerializer<CtsDailyScanMessage> _serializer = serializer;
    private readonly CtsDailyScanOrchestrator _orchestrator = orchestrator;
    private readonly DataBridgeScanConfiguration _dataBridgeScanConfiguration = dataBridgeScanConfiguration;

    public async Task<MessageType> Handle(ProcessCtsDailyScanMessageCommand request, CancellationToken cancellationToken)
    {
        var message = request.Message;

        ArgumentNullException.ThrowIfNull(message);

        var messagePayload = _serializer.Deserialize(message)
            ?? throw new NonRetryableException($"Deserialisation failed or the message payload was null for " +
            $"messageType: {typeof(CtsDailyScanMessage).Name}," +
            $"messageId: {message.MessageId}," +
            $"correlationId: {message.CorrelationId}");

        var context = new CtsDailyScanContext
        {
            CurrentDateTime = DateTime.UtcNow,
            UpdatedSinceDateTime = DateTime.UtcNow.AddHours(-24),
            PageSize = _dataBridgeScanConfiguration.QueryPageSize,
        };

        await _orchestrator.ExecuteAsync(context, cancellationToken);

        return await Task.FromResult(messagePayload!);
    }
}