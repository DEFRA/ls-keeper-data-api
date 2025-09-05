using Microsoft.AspNetCore.Builder;

namespace KeeperData.Api.Tests.Component.Config;

public class EnvironmentTest
{

    [Fact]
    public void IsNotDevModeByDefault()
    {
        var builder = WebApplication.CreateEmptyBuilder(new WebApplicationOptions());
        var isDev = KeeperData.Api.Config.Environment.IsDevMode(builder);
        Assert.False(isDev);
    }
}