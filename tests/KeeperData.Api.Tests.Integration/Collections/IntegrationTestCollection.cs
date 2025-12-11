namespace KeeperData.Api.Tests.Integration.Collections;

/// <summary>
/// Collection definition to prevent parallel execution of conflicting tests
/// </summary>
[CollectionDefinition("Integration Tests")]
public class IntegrationTestCollection : ICollectionFixture<IntegrationTestFixture>
{
}