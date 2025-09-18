using Amazon.SQS.Model;

namespace KeeperData.Core.Messaging.Serializers;

public interface IMessageSerializer<out T>
{
    T? Deserialize(Message message);
}
