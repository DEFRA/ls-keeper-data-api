using KeeperData.Tests.Common.Fakes;
using MongoDB.Driver;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Servers;
using System.Net;
using System.Reflection;

namespace KeeperData.Tests.Common.Factories;

public static class MongoExceptionFactory
{
    public static MongoBulkWriteException<object> CreateMongoBulkWriteException()
    {
        // Use the shared FakeBulkWriteResult
        var bulkWriteResult = new FakeBulkWriteResult<object>();

        var connectionId = new ConnectionId(new ServerId(new ClusterId(), new DnsEndPoint("localhost", 27017)), 1);
        var writeErrors = new List<BulkWriteError>();

        // Use Reflection to access the internal constructor of WriteConcernError
        var writeConcernErrorCtor = typeof(WriteConcernError)
            .GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
            .First();

        var writeConcernError = (WriteConcernError)writeConcernErrorCtor.Invoke([
            1, "CodeName", "Message", new MongoDB.Bson.BsonDocument(), new List<string>()
        ]);

        return new MongoBulkWriteException<object>(
            connectionId,
            bulkWriteResult,
            writeErrors,
            writeConcernError,
            new List<WriteModel<object>>());
    }
}