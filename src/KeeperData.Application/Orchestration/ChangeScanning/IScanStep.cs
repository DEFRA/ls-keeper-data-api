namespace KeeperData.Application.Orchestration.ChangeScanning;

public interface IScanStep<TContext>
{
    Task ExecuteAsync(TContext context, CancellationToken cancellationToken);
}
