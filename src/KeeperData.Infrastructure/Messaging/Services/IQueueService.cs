using Amazon.SQS.Model;
using KeeperData.Core.DeadLetter;

namespace KeeperData.Infrastructure.Messaging.Services
{
    public interface IQueueService
    {
        Task<bool> MoveToDeadLetterQueueAsync(Message message, string queueUrl, Exception ex, CancellationToken cancellationToken);
        Task<QueueStats> GetQueueStatsAsync(string queueUrl, CancellationToken ct = default);
        Task<DeadLetterMessagesResult> PeekDeadLetterMessagesAsync(int maxMessages, CancellationToken ct = default);
        Task<RedriveSummary> RedriveDeadLetterMessagesAsync(int maxMessages, CancellationToken ct = default);
        Task<PurgeResult> PurgeDeadLetterQueueAsync(CancellationToken ct = default);
    }
}