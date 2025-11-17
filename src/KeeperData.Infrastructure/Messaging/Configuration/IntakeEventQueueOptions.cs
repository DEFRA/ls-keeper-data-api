namespace KeeperData.Infrastructure.Messaging.Configuration;

public record IntakeEventQueueOptions : QueueConsumerOptions
{
    public string? DeadLetterQueueUrl { get; init; }
}