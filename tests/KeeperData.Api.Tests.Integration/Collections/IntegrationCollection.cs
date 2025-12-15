using KeeperData.Api.Tests.Integration.Fixtures;

namespace KeeperData.Api.Tests.Integration.Collections;

[CollectionDefinition("Integration")]
public class IntegrationCollection :
    ICollectionFixture<MongoDbFixture>,
    ICollectionFixture<LocalStackFixture>,
    ICollectionFixture<ApiContainerFixture>
{ }