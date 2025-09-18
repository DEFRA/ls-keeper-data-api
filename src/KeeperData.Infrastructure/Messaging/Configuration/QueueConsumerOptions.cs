namespace KeeperData.Infrastructure.Messaging.Configuration;

public record QueueConsumerOptions
{
    public required string QueueUrl { get; init; }
    public int MaxNumberOfMessages { get; init; }
    public int WaitTimeSeconds { get; init; }
    public bool Disabled { get; set; } = false;
}