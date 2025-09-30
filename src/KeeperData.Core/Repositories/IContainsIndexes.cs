using MongoDB.Bson;
using MongoDB.Driver;

namespace KeeperData.Core.Repositories;

public interface IContainsIndexes
{
    static abstract IEnumerable<CreateIndexModel<BsonDocument>> GetIndexModels();
}
