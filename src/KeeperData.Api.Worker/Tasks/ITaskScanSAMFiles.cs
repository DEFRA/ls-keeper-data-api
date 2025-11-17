namespace KeeperData.Api.Worker.Tasks;

public interface ITaskScanSAMFiles : ITask
{

    /// <summary>
    /// Starts the scan process asynchronously and returns immediately after acquiring the lock.
    /// </summary>
    /// <param name="sourceType"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<Guid?> StartAsync(string sourceType, CancellationToken cancellationToken = default);
}