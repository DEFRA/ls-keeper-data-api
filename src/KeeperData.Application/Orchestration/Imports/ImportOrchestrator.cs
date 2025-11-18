namespace KeeperData.Application.Orchestration.Imports;

public abstract class ImportOrchestrator<TContext>(IEnumerable<IImportStep<TContext>> steps)
{
    private readonly IEnumerable<IImportStep<TContext>> _steps = steps;

    public async Task ExecuteAsync(TContext context, CancellationToken cancellationToken)
    {
        foreach (var step in _steps)
        {
            await step.ExecuteAsync(context, cancellationToken);
        }
    }
}