namespace KeeperData.Core.Messaging.Consumers;

public interface IQueuePoller
{
    Task StartAsync(CancellationToken token);
    Task StopAsync(CancellationToken token);
}
