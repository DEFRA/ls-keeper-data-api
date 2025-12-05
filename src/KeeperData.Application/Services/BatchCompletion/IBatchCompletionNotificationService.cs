namespace KeeperData.Application.Services.BatchCompletion;

public interface IBatchCompletionNotificationService
{
    Task NotifyBatchCompletionAsync<TContext>(TContext context, CancellationToken cancellationToken = default)
        where TContext : class;
}