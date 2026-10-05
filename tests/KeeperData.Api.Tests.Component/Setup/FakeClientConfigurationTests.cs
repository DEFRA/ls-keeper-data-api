using FluentAssertions;
using KeeperData.Api.Setup;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;

namespace KeeperData.Api.Tests.Component.Setup;

public class FakeClientConfigurationTests
{
    [Theory]
    [InlineData("Staging")]
    [InlineData("Production")]
    public void ConfigureApi_WhenFakeClientIsEnabledOutsideDevelopment_Throws(string environmentName)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ApiClients:DataBridgeApi:UseFakeClient"] = "true"
            })
            .Build();
        var environment = new Mock<IHostEnvironment>();
        environment.SetupGet(x => x.EnvironmentName).Returns(environmentName);
        var services = new ServiceCollection();

        var act = () => services.ConfigureApi(configuration, environment.Object);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*UseFakeClient can only be enabled in the Development environment*");
    }
}