using KeeperData.Core.Providers;

namespace KeeperData.Application.Providers;

public class RealDelayProvider : IDelayProvider
{
    public Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken) =>
        Task.Delay(delay, cancellationToken);
}
