namespace KeeperData.Application.Orchestration.Updates;

public interface IUpdateStep<TContext>
{
    Task ExecuteAsync(TContext context, CancellationToken cancellationToken);
}