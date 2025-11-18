namespace KeeperData.Api.Worker.Tasks;

public interface ISamBulkScanTask : ITask
{
    Task<Guid?> StartAsync(CancellationToken cancellationToken = default);
}