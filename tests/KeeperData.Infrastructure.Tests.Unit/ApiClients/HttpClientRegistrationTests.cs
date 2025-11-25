using FluentAssertions;
using KeeperData.Infrastructure.ApiClients.Setup;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KeeperData.Infrastructure.Tests.Unit.ApiClients;

public class HttpClientRegistrationTests
{
    private const string DataBridgeApiBaseUrl = "http://localhost:5560";

    [Fact]
    public void AddApiClientDependencies_ShouldAddAuthorizationHeader_WhenBridgeApiSubscriptionKeyIsSet()
    {
        // Arrange: build configuration with ApiClients section
        var inMemorySettings = new Dictionary<string, string?>
        {
            { "ApiClients:DataBridgeApi:BaseUrl", DataBridgeApiBaseUrl },
            { "ApiClients:DataBridgeApi:BridgeApiSubscriptionKey", "XYZ" },
            { "ApiClients:DataBridgeApi:ResiliencePolicy:Retries", "1" },
            { "ApiClients:DataBridgeApi:ResiliencePolicy:BaseDelaySeconds", "1" },
            { "ApiClients:DataBridgeApi:ResiliencePolicy:UseJitter", "false" },
            { "ApiClients:DataBridgeApi:ResiliencePolicy:TimeoutPeriodSeconds", "5" }
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        var services = new ServiceCollection();
        services.AddApiClientDependencies(configuration);

        var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IHttpClientFactory>();
        var client = factory.CreateClient("DataBridgeApi");

        client.DefaultRequestHeaders.Authorization.Should().NotBeNull();
        client.DefaultRequestHeaders.Authorization!.Scheme.Should().Be("ApiKey");
        client.DefaultRequestHeaders.Authorization!.Parameter.Should().Be("XYZ");
    }
}
