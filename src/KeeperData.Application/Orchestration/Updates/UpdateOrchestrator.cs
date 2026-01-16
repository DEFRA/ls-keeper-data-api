namespace KeeperData.Application.Orchestration.Updates;

public abstract class UpdateOrchestrator<TContext>(IEnumerable<IUpdateStep<TContext>> steps)
{
    private readonly IEnumerable<IUpdateStep<TContext>> _steps = steps;

    public virtual async Task ExecuteAsync(TContext context, CancellationToken cancellationToken)
    {
        foreach (var step in _steps)
        {
            await step.ExecuteAsync(context, cancellationToken);
        }
    }
}