namespace KeeperData.Application.Orchestration.ChangeScanning;

public abstract class ScanOrchestrator<TContext>(IEnumerable<IScanStep<TContext>> steps)
    where TContext : class
{
    private readonly IEnumerable<IScanStep<TContext>> _steps = steps;

    public virtual async Task ExecuteAsync(TContext context, CancellationToken cancellationToken)
    {
        foreach (var step in _steps)
        {
            await step.ExecuteAsync(context, cancellationToken);
        }
    }
}