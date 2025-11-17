namespace KeeperData.Api.Worker.Tasks;

public interface ITaskScanCTSFiles : ITask
{
    Task<Guid?> StartAsync(CancellationToken cancellationToken = default);
}