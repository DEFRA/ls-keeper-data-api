public interface IMongoDbPreproductionService
{
    Task WipeCollection(string collection);
}