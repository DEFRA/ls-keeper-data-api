namespace KeeperData.Core.Repositories;

public interface IDeletableEntity
{
    bool Deleted { get; }
}