namespace KeeperData.Infrastructure.Services;

public interface IMongoDbPreproductionService
{
    Task<string> DropCollection(string collection);
}