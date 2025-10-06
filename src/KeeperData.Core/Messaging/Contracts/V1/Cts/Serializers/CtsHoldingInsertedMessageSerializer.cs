using KeeperData.Core.Messaging.Serializers;
using System.Text.Json;

namespace KeeperData.Core.Messaging.Contracts.V1.Cts.Serializers;

public class CtsHoldingInsertedMessageSerializer : IUnwrappedMessageSerializer<CtsHoldingInsertedMessage>
{
    public CtsHoldingInsertedMessage? Deserialize(UnwrappedMessage message)
    {
        var messageBody = JsonSerializer.Deserialize(message.Payload, CtsHoldingInsertedMessageSerializerContext.Default.CtsHoldingInsertedMessage);
        return messageBody;
    }
}
