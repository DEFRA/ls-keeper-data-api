using FluentAssertions;
using KeeperData.Tests.Common.Utilities;
using System.Net;

namespace KeeperData.Api.Tests.Component.Authentication;

public class AuthenticationHandlerTests
{
    private const string BasicApiKey = "ApiKey";
    private const string BasicSecret = "integration-test-secret";

    [Fact]
    public async Task WhenApiGatewayExists_JwtIsDefaultScheme()
    {
        var factory = new AppWebApplicationFactory(new Dictionary<string, string?>
        {
            ["AuthenticationConfiguration:EnableApiKey"] = "true",
            ["AuthenticationConfiguration:ApiGatewayExists"] = "true",
            ["AuthenticationConfiguration:Authority"] = "https://fake",
            ["Acl:Clients:ApiKey:Type"] = BasicApiKey,
            ["Acl:Clients:ApiKey:Secret"] = BasicSecret
        },
        useFakeAuth: true);

        var client = factory.CreateClient();
        client.AddJwt();

        var response = await client.GetAsync("/api/parties");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ApiKeyOnlyEndpoint_AllowsApiKey_WhenJwtIsDefault()
    {
        var factory = new AppWebApplicationFactory(new Dictionary<string, string?>
        {
            ["AuthenticationConfiguration:EnableApiKey"] = "true",
            ["AuthenticationConfiguration:ApiGatewayExists"] = "false",
            ["AuthenticationConfiguration:Authority"] = "https://fake",
            ["Acl:Clients:ApiKey:Type"] = BasicApiKey,
            ["Acl:Clients:ApiKey:Secret"] = BasicSecret,
            ["Acl:Clients:ApiKey:Scopes:0"] = "access"
        });

        var client = factory.CreateClient();
        client.AddBasicApiKey(BasicApiKey, BasicSecret);

        var response = await client.GetAsync("/api/parties");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task JwtOnlyEndpoint_RejectsApiKey_WhenJwtIsDefault()
    {
        var factory = new AppWebApplicationFactory(new Dictionary<string, string?>
        {
            ["AuthenticationConfiguration:EnableApiKey"] = "false",
            ["AuthenticationConfiguration:ApiGatewayExists"] = "true",
            ["AuthenticationConfiguration:Authority"] = "https://fake"
        });

        var client = factory.CreateClient();
        client.AddBasicApiKey(BasicApiKey, BasicSecret);

        var response = await client.GetAsync("/api/parties");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task WhenApiGatewayDoesNotExist_ApiKeyIsDefault()
    {
        var factory = new AppWebApplicationFactory(new Dictionary<string, string?>
        {
            ["AuthenticationConfiguration:EnableApiKey"] = "true",
            ["AuthenticationConfiguration:ApiGatewayExists"] = "false",
            ["Acl:Clients:ApiKey:Type"] = BasicApiKey,
            ["Acl:Clients:ApiKey:Secret"] = BasicSecret
        });

        var client = factory.CreateClient();
        client.AddBasicApiKey(BasicApiKey, BasicSecret);

        var response = await client.GetAsync("/api/parties");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task WhenApiGatewayDoesNotExist_JwtIsIgnored()
    {
        var factory = new AppWebApplicationFactory(new Dictionary<string, string?>
        {
            ["AuthenticationConfiguration:EnableApiKey"] = "true",
            ["AuthenticationConfiguration:ApiGatewayExists"] = "false"
        });

        var client = factory.CreateClient();
        client.AddJwt();

        var response = await client.GetAsync("/api/parties");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
