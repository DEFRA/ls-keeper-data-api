using KeeperData.Api.Tests.Integration.Helpers;

namespace KeeperData.Api.Tests.Integration;

[CollectionDefinition("Integration")]
public class IntegrationCollection :
    ICollectionFixture<MongoDbFixture>,
    ICollectionFixture<LocalStackFixture>,
    ICollectionFixture<ApiContainerFixture>
{ }