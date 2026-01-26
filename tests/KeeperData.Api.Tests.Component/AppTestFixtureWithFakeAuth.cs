namespace KeeperData.Api.Tests.Component;

public class AppTestFixtureWithFakeAuth : AppTestFixture
{
    public AppTestFixtureWithFakeAuth()
        : base(useFakeAuth: true)
    {
    }
}