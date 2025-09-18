using Amazon.SQS.Model;
using KeeperData.Core.Messaging.Serializers;
using System.Text.Json;

namespace KeeperData.Core.Messaging.Contracts.Serializers;

public class SnsEnvelopeSerializer : IMessageSerializer<SnsEnvelope>
{
    public SnsEnvelope? Deserialize(Message message)
    {
        try
        {
            var envelope = JsonSerializer.Deserialize(message.Body, SnsEnvelopeSerializerContext.Default.SnsEnvelope);
            return envelope;
        }
        catch
        { 
            return null;
        }        
    }
}
