namespace KeeperData.Infrastructure.Messaging.Configuration;

public record IntakeEventQueueOptions : QueueConsumerOptions
{
    public string? DeadLetterQueueUrl { get; init; }
    public int MaxReceiveCount { get; init; } = 3;
    public int MaxPeekMessages { get; init; } = 100;
    public int MaxRedriveMessages { get; init; } = 100;
}