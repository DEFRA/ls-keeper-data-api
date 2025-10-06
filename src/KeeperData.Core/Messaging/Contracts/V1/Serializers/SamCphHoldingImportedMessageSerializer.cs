using KeeperData.Core.Messaging.Serializers;
using System.Text.Json;

namespace KeeperData.Core.Messaging.Contracts.V1.Serializers;

public class SamCphHoldingImportedMessageSerializer : IUnwrappedMessageSerializer<SamCphHoldingImportedMessage>
{
    public SamCphHoldingImportedMessage? Deserialize(UnwrappedMessage message)
    {
        var messageBody = JsonSerializer.Deserialize(message.Payload, SamCphHoldingImportedMessageSerializerContext.Default.SamCphHoldingImportedMessage);
        return messageBody;
    }
}