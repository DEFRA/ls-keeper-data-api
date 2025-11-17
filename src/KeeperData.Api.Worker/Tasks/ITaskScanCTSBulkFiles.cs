namespace KeeperData.Api.Worker.Tasks;

public interface ITaskScanCTSBulkFiles : ITask
{
    Task<Guid?> StartAsync(CancellationToken cancellationToken = default);
}