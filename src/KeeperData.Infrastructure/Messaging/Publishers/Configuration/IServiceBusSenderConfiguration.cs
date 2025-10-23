namespace KeeperData.Infrastructure.Messaging.Publishers.Configuration;

public interface IServiceBusSenderConfiguration
{
    TopicConfiguration IntakeEventsTopic { get; init; }
}
