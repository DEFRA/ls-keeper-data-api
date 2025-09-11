using Amazon.SQS;
using KeeperData.Core.Models;
using KeeperData.Infrastructure.Queues;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KeeperData.Core.Consumers;

public class IntakeEventConsumer(
    ILogger<IntakeEventConsumer> logger,
    IIntakeEventRepository intakeEventRepository,
    IAmazonSQS sqsClient,
    IOptions<IntakeEventConsumerOptions> options)
    : QueueConsumerBase<IntakeEventModel>(logger, sqsClient, options)
{
    protected override Task ProcessMessageAsync(IntakeEventModel payload, CancellationToken cancellationToken)
    {
        return intakeEventRepository.CreateAsync(payload, cancellationToken);
    }
}

public record IntakeEventConsumerOptions : QueueConsumerOptions;

// Everything below here would normally be elsewhere in the application

public interface IIntakeEventRepository
{
    public Task CreateAsync(IntakeEventModel payload, CancellationToken cancellationToken);
}

public class IntakeEventRepository : IIntakeEventRepository
{
    public Task CreateAsync(IntakeEventModel payload, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}