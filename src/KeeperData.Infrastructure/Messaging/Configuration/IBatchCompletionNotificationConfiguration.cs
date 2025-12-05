namespace KeeperData.Infrastructure.Messaging.Configuration;

public interface IBatchCompletionNotificationConfiguration
{
    TopicConfiguration BatchCompletionEventsTopic { get; init; }
}