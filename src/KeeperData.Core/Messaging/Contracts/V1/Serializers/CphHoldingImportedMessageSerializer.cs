using KeeperData.Core.Messaging.Serializers;
using System.Text.Json;

namespace KeeperData.Core.Messaging.Contracts.V1.Serializers;

public class CphHoldingImportedMessageSerializer : IUnwrappedMessageSerializer<CphHoldingImportedMessage>
{
    public CphHoldingImportedMessage? Deserialize(UnwrappedMessage message)
    {
        var messageBody = JsonSerializer.Deserialize(message.Payload, CphHoldingImportedMessageSerializerContext.Default.CphHoldingImportedMessage);
        return messageBody;
    }
}