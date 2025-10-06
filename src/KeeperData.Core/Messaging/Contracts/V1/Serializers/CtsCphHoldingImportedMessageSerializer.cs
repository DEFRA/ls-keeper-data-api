using KeeperData.Core.Messaging.Serializers;
using System.Text.Json;

namespace KeeperData.Core.Messaging.Contracts.V1.Serializers;

public class CtsCphHoldingImportedMessageSerializer : IUnwrappedMessageSerializer<CtsCphHoldingImportedMessage>
{
    public CtsCphHoldingImportedMessage? Deserialize(UnwrappedMessage message)
    {
        var messageBody = JsonSerializer.Deserialize(message.Payload, CtsCphHoldingImportedMessageSerializerContext.Default.CtsCphHoldingImportedMessage);
        return messageBody;
    }
}
