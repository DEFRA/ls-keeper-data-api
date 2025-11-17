namespace KeeperData.Core.Messaging.MessagePublishers;

public interface IQueueClient
{
    string ClientName { get; }
}