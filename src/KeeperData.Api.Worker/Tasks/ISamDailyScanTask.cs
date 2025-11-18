namespace KeeperData.Api.Worker.Tasks;

public interface ISamDailyScanTask : ITask
{
    Task<Guid?> StartAsync(CancellationToken cancellationToken = default);
}