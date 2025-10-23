using KeeperData.Core.Messaging.MessagePublishers;

namespace KeeperData.Infrastructure.Messaging.Publishers.Clients;

public class IntakeEventsQueueClient : IQueueClient
{
    public string ClientName => GetType().Name;
}