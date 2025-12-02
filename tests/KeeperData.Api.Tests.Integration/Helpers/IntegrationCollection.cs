namespace KeeperData.Api.Tests.Integration.Helpers;

[CollectionDefinition("Integration")]
public class IntegrationCollection : 
    ICollectionFixture<MongoDbFixture>,
    ICollectionFixture<LocalStackFixture>,
    ICollectionFixture<ApiContainerFixture>
{ }
