namespace KeeperData.Api.Worker.Tasks;

public interface IDailyScanTask : IScanTask
{
    Task<Guid?> StartAsync(int? customSinceHours, CancellationToken cancellationToken = default);
}