namespace KeeperData.Infrastructure.Messaging.Configuration;

public record IntakeEventFifoQueueOptions : QueueConsumerOptions
{
    public string? DeadLetterQueueUrl { get; init; }
}