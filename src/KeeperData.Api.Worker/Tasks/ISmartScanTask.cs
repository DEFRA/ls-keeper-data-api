namespace KeeperData.Api.Worker.Tasks;

public interface ISmartScanTask : ITask
{
    Task<Guid?> StartAsync(bool forceBulk = false, int? sinceHours = null, CancellationToken cancellationToken = default);
}
