namespace KeeperData.Api.Tests.Integration;

[Trait("Dependence", "localstack")]
public class IntegrationTestBase
{
    [Fact]
    public void TestShouldOnlyRunWhenLocalstackIsAvailable()
    {
    }
}