namespace KeeperData.Infrastructure.Messaging.Publishers.Configuration;

public record QueueConfiguration
{
    public string QueueUrl { get; init; } = string.Empty;
}