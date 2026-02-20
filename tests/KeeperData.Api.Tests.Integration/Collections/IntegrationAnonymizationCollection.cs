using KeeperData.Api.Tests.Integration.Fixtures;

namespace KeeperData.Api.Tests.Integration.Collections;

[CollectionDefinition("IntegrationAnonymization")]
public class IntegrationAnonymizationCollection :
    ICollectionFixture<MongoDbAnonymousFixture>,
    ICollectionFixture<LocalStackAnonymousFixture>,
    ICollectionFixture<ApiAnonymousContainerFixture>
{ }