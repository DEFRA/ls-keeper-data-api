namespace KeeperData.Application.Orchestration.Updates;

public interface IUpdateStep<in TContext>
{
    Task ExecuteAsync(TContext context, CancellationToken cancellationToken);
}