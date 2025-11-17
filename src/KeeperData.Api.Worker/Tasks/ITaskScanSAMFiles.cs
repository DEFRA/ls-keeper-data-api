namespace KeeperData.Api.Worker.Tasks;

public interface ITaskScanSAMFiles : ITask
{
    Task<Guid?> StartAsync(CancellationToken cancellationToken = default);
}