namespace KeeperData.Core.Messaging.MessagePublishers.Clients;

public class IntakeEventsQueueClient : IQueueClient
{
    public string ClientName => GetType().Name;
}