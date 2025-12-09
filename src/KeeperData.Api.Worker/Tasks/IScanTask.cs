namespace KeeperData.Api.Worker.Tasks;

public interface IScanTask : ITask
{
    Task<Guid?> StartAsync(CancellationToken cancellationToken = default);
}