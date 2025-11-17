namespace KeeperData.Api.Worker.Tasks;

public interface ITaskScanSAMBulkFiles : ITask
{
    Task<Guid?> StartAsync(CancellationToken cancellationToken = default);
}