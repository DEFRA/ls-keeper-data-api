using KeeperData.Core.Orchestration;

namespace KeeperData.Core.Services;

public interface IBatchCompletionNotificationService
{
    Task NotifyBatchCompletionAsync<TContext>(TContext context, CancellationToken cancellationToken = default)
        where TContext : IScanContext;
}