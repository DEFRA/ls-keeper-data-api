namespace KeeperData.Api.Worker.Tasks;

public interface ITask
{
    Task RunAsync(CancellationToken cancellationToken);
}