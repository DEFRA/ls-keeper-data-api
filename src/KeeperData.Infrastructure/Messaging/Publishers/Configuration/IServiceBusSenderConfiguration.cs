namespace KeeperData.Infrastructure.Messaging.Publishers.Configuration;

public interface IServiceBusSenderConfiguration
{
    QueueConfiguration IntakeEventQueue { get; init; }
}