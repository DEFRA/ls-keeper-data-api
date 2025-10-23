using KeeperData.Core.Messaging.MessagePublishers;

namespace KeeperData.Infrastructure.Messaging.Publishers.Clients;

public class IntakeEventsTopicClient : ITopicClient
{
    public string ClientName => GetType().Name;
}
