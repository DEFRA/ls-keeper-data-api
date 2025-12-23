namespace KeeperData.Core.Repositories;

public interface IMongoDbPreproductionService
{
    Task<string> WipeCollection(string collection);
}