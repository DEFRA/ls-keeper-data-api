namespace KeeperData.Core.Repositories;

public interface IEntity
{
    string Id { get; }
    int LastUpdatedBatchId { get; }
}