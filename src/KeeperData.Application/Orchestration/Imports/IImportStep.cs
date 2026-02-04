namespace KeeperData.Application.Orchestration.Imports;

public interface IImportStep<in TContext>
{
    Task ExecuteAsync(TContext context, CancellationToken cancellationToken);
}