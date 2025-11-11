namespace KeeperData.Application.Orchestration.ChangeScanning;

public abstract class ScanOrchestrator<TContext>(IEnumerable<IScanStep<TContext>> steps)
{
    private readonly IEnumerable<IScanStep<TContext>> _steps = steps;

    public async Task ExecuteAsync(TContext context, CancellationToken cancellationToken)
    {
        foreach (var step in _steps)
        {
            await step.ExecuteAsync(context, cancellationToken);
        }
    }
}
