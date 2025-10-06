using KeeperData.Core.Messaging.Contracts.V1.Serializers;
using KeeperData.Core.Messaging.Serializers;
using System.Text.Json;

namespace KeeperData.Core.Messaging.Contracts.V1.Sam.Serializers;

public class SamHoldingInsertedMessageSerializer : IUnwrappedMessageSerializer<SamHoldingInsertedMessage>
{
    public SamHoldingInsertedMessage? Deserialize(UnwrappedMessage message)
    {
        var messageBody = JsonSerializer.Deserialize(message.Payload, SamHoldingInsertedMessageSerializerContext.Default.SamHoldingInsertedMessage);
        return messageBody;
    }
}