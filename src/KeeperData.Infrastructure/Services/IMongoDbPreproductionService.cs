namespace KeeperData.Infrastructure.Services;

public interface IMongoDbPreproductionService
{
    Task<string> WipeCollection(string collection);
}