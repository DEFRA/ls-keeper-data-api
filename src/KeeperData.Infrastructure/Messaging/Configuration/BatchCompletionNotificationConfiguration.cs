namespace KeeperData.Infrastructure.Messaging.Configuration;

public class BatchCompletionNotificationConfiguration : IBatchCompletionNotificationConfiguration
{
    public TopicConfiguration BatchCompletionEventsTopic { get; init; } = new();
}