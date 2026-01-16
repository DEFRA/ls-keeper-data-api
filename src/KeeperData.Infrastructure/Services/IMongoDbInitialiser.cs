namespace KeeperData.Infrastructure.Services
{
    public interface IMongoDbInitialiser
    {
        public Task Initialise(Type type);
    }
}