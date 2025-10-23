namespace KeeperData.Infrastructure.Messaging.Publishers.Configuration;

public class ServiceBusSenderConfiguration : IServiceBusSenderConfiguration
{
    public QueueConfiguration IntakeEventQueue { get; init; } = new();
}