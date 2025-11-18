namespace KeeperData.Api.Worker.Tasks;

public interface ICtsBulkScanTask : ITask
{
    Task<Guid?> StartAsync(CancellationToken cancellationToken = default);
}