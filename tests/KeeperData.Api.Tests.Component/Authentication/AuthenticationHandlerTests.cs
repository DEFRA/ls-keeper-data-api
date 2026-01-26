using FluentAssertions;
using KeeperData.Api.Worker.Tasks;
using KeeperData.Tests.Common.Utilities;
using Moq;
using System.Net;

namespace KeeperData.Api.Tests.Component.Authentication;

public class AuthenticationHandlerTests
{
    private const string BasicApiKey = "ApiKey";
    private const string BasicSecret = "integration-test-secret";

    [Fact]
    public async Task WhenApiGatewayExists_JwtSchemeSucceeds()
    {
        var factory = new AppWebApplicationFactory(useFakeAuth: true);

        var client = factory.CreateClient();
        client.AddJwt();

        var response = await client.GetAsync("/api/parties");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task WhenApiKeyEnabled_BasicSchemeSucceeds()
    {
        var factory = new AppWebApplicationFactory(useFakeAuth: true);

        var client = factory.CreateClient();
        client.AddBasicApiKey(BasicApiKey, BasicSecret);

        var response = await client.GetAsync("/api/parties");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task WhenApiGatewayExists_ButEndpointIsBasicOnly_JwtSchemeFails()
    {
        var configurationOverrides = new Dictionary<string, string?>
        {
            ["BulkScanEndpointsEnabled"] = "true"
        };

        var ctsBulkScanTaskMock = new Mock<ICtsBulkScanTask>();
        ctsBulkScanTaskMock.Setup(x => x.StartAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Guid.NewGuid());

        var factory = new AppWebApplicationFactory(configurationOverrides, useFakeAuth: true);
        factory.OverrideServiceAsSingleton(ctsBulkScanTaskMock.Object);

        var client = factory.CreateClient();
        client.AddJwt();

        var response = await client.PostAsync("/api/import/startCtsBulkScan", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task WhenApiKeyEnabled_AndEndpointIsBasicOnly_BasicSchemeSucceeds()
    {
        var configurationOverrides = new Dictionary<string, string?>
        {
            ["BulkScanEndpointsEnabled"] = "true"
        };

        var ctsBulkScanTaskMock = new Mock<ICtsBulkScanTask>();
        ctsBulkScanTaskMock.Setup(x => x.StartAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Guid.NewGuid());

        var factory = new AppWebApplicationFactory(configurationOverrides, useFakeAuth: true);
        factory.OverrideServiceAsSingleton(ctsBulkScanTaskMock.Object);

        var client = factory.CreateClient();
        client.AddBasicApiKey(BasicApiKey, BasicSecret);

        var response = await client.PostAsync("/api/import/startCtsBulkScan", null);

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
    }
}