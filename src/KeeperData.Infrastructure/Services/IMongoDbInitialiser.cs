namespace KeeperData.Infrastructure.Services
{
    public interface IMongoDbInitialiser
    {
        Task Initialise(Type type);

        Task DropAllCollectionsAsync();
    }
}