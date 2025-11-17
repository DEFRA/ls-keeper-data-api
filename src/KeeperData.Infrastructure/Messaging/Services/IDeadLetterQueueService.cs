using Amazon.SQS.Model;

namespace KeeperData.Infrastructure.Messaging.Services
{
    public interface IDeadLetterQueueService
    {
        Task<bool> MoveToDeadLetterQueueAsync(Message message, string queueUrl, Exception ex, CancellationToken cancellationToken);
    }
}