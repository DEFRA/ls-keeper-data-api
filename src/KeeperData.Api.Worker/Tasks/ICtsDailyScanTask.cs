namespace KeeperData.Api.Worker.Tasks;

public interface ICtsDailyScanTask : ITask
{
    Task<Guid?> StartAsync(CancellationToken cancellationToken = default);
}