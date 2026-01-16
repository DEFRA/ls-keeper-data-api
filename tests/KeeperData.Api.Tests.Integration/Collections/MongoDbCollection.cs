using KeeperData.Api.Tests.Integration.Fixtures;

namespace KeeperData.Api.Tests.Integration.Collections;

[CollectionDefinition("MongoDB")]
public class MongoDbCollection : ICollectionFixture<MongoDbFixture> { }