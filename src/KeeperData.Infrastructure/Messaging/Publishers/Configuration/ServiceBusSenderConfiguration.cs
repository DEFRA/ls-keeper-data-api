namespace KeeperData.Infrastructure.Messaging.Publishers.Configuration;

public class ServiceBusSenderConfiguration : IServiceBusSenderConfiguration
{
    public TopicConfiguration IntakeEventsTopic { get; init; } = new();
}
