using System.Net;
using Amazon.Extensions.NETCore.Setup;
using Amazon.Runtime;
using FluentAssertions;
using KeeperData.Infrastructure.ApiClients.Setup;
using KeeperData.Infrastructure.ApiClients.Sts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KeeperData.Infrastructure.Tests.Unit.ApiClients;

public class HttpClientRegistrationTests
{
    private const string DataBridgeApiBaseUrl = "http://localhost:5560";

    [Fact]
    public void AddApiClientDependencies_ShouldAddAuthorizationHeader_WhenBridgeApiSubscriptionKeyIsSet()
    {
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

    [Fact]
    public async Task AddApiClientDependencies_WhenUseStsAuthenticationIsTrue_ShouldAttachAwsStsHeaderInsteadOfApiKey()
    {
        var inMemorySettings = new Dictionary<string, string?>
        {
            { "ApiClients:DataBridgeApi:BaseUrl", DataBridgeApiBaseUrl },
            { "ApiClients:DataBridgeApi:BridgeApiSubscriptionKey", "XYZ" },
            { "ApiClients:DataBridgeApi:UseStsAuthentication", "true" },
            { "ApiClients:DataBridgeApi:StsEndpoint", "https://sts.eu-west-2.amazonaws.com" },
            { "ApiClients:DataBridgeApi:StsRegion", "eu-west-2" },
            { "ApiClients:DataBridgeApi:StsExpiresSeconds", "60" },
            { "ApiClients:DataBridgeApi:XApiKey", "gateway-key" },
            { "ApiClients:DataBridgeApi:ResiliencePolicy:Retries", "1" },
            { "ApiClients:DataBridgeApi:ResiliencePolicy:BaseDelaySeconds", "1" },
            { "ApiClients:DataBridgeApi:ResiliencePolicy:UseJitter", "false" },
            { "ApiClients:DataBridgeApi:ResiliencePolicy:TimeoutPeriodSeconds", "5" }
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        HttpRequestMessage? capturedRequest = null;
        var services = new ServiceCollection();
        services.AddSingleton(new AWSOptions
        {
            Credentials = new BasicAWSCredentials("AKIAEXAMPLE", "secret-key")
        });
        services.AddApiClientDependencies(configuration);
        services.AddHttpClient("DataBridgeApi")
            .ConfigurePrimaryHttpMessageHandler(() => new RecordingPrimaryHandler(req =>
            {
                capturedRequest = req;
                return new HttpResponseMessage(HttpStatusCode.OK);
            }));

        var provider = services.BuildServiceProvider();
        provider.GetService<IStsTokenProvider>().Should().NotBeNull();

        var client = provider.GetRequiredService<IHttpClientFactory>().CreateClient("DataBridgeApi");
        client.DefaultRequestHeaders.Authorization.Should().BeNull("ApiKey static header must not be set when UseStsAuthentication is true");
        client.DefaultRequestHeaders.GetValues("x-api-key").Should().ContainSingle("gateway-key");

        var response = await client.GetAsync("/api/etl/sqlite/cphs/latest");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        capturedRequest.Should().NotBeNull();
        capturedRequest!.Headers.Authorization.Should().NotBeNull();
        capturedRequest.Headers.Authorization!.Scheme.Should().Be(StsAuthenticationMessageHandler.SchemeName);
        capturedRequest.Headers.Authorization.Parameter.Should().NotBeNullOrWhiteSpace();
    }

    private sealed class RecordingPrimaryHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(responder(request));
    }
}