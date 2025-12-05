namespace KeeperData.Core.Messaging.MessagePublishers.Clients;

public class BatchCompletionTopicClient : ITopicClient
{
    public string ClientName => GetType().Name;
}