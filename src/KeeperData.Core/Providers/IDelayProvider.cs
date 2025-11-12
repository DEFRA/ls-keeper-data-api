namespace KeeperData.Core.Providers;

public interface IDelayProvider
{
    Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken);
}
