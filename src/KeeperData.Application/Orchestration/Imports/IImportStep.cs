namespace KeeperData.Application.Orchestration.Imports;

public interface IImportStep<TContext>
{
    Task ExecuteAsync(TContext context, CancellationToken cancellationToken);
}