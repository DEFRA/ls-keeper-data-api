namespace KeeperData.Application.Orchestration.ChangeScanning;

public interface IScanStep<in TContext>
{
    Task ExecuteAsync(TContext context, CancellationToken cancellationToken);
}