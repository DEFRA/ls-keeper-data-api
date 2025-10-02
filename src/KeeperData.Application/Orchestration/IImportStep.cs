namespace KeeperData.Application.Orchestration;

public interface IImportStep<TContext>
{
    Task ExecuteAsync(TContext context, CancellationToken cancellationToken);
}