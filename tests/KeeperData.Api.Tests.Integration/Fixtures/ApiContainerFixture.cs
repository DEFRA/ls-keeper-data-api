using KeeperData.Api.Tests.Integration.Helpers;

namespace KeeperData.Api.Tests.Integration.Fixtures;

using DotNet.Testcontainers.Builders;
using KeeperData.Tests.Common.Utilities;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;
using IContainer = DotNet.Testcontainers.Containers.IContainer;

public class ApiContainerFixture : IAsyncLifetime
{
    public IContainer ApiContainer { get; private set; } = null!;
    public HttpClient HttpClient { get; private set; } = null!;

    public string NetworkName { get; } = "integration-test-network";
    private const string BasicApiKey = "ApiKey";
    private const string BasicSecret = "integration-test-secret";

    private readonly int _hostPort;
    private readonly int _containerPort;

    public ApiContainerFixture()
    {
        _hostPort = 5555;
        _containerPort = 5555;
    }

    public async Task InitializeAsync()
    {
        DockerNetworkHelper.EnsureNetworkExists(NetworkName);

        var containerBuilder = new ContainerBuilder("keeperdata_api:latest")
          .WithName("keeperdata_api")
          .WithPortBinding(_hostPort, _containerPort)
          .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
          .WithEnvironment("ASPNETCORE_HTTP_PORTS", _containerPort.ToString())
          .WithEnvironment("Mongo__DatabaseUri", "mongodb://testuser:testpass@mongo:27017/ls-keeper-data-api?authSource=admin")
          .WithEnvironment("ApiClients__DataBridgeApi__BaseUrl", "http://localhost:5560/")
          .WithEnvironment("ApiClients__DataBridgeApi__BridgeApiSubscriptionKey", "")
          .WithEnvironment("ApiClients__DataBridgeApi__ServiceName", "")
          .WithEnvironment("ApiClients__DataBridgeApi__XApiKey", "")
          .WithEnvironment("ApiClients__DataBridgeApi__UseFakeClient", "true")
          .WithEnvironment("LegacyEndpointsEnabled", "true")
          .WithEnvironment("AWS_REGION", "eu-west-2")
          .WithEnvironment("AWS_DEFAULT_REGION", "eu-west-2");

        ApiContainer = containerBuilder
              .WithNetwork(NetworkName)
              .WithNetworkAliases("keeperdata_api")
              .WithWaitStrategy(Wait.ForUnixContainer()
                  .UntilHttpRequestIsSucceeded(req => req.ForPort((ushort)_containerPort).ForPath("/health"), o => o.WithTimeout(TimeSpan.FromSeconds(60))))
              .Build();

        try
        {
            await ApiContainer.StartAsync();
        }
        catch (Exception ex)
        {
            var (stdout, stderr) = await ApiContainer.GetLogsAsync();
            throw new InvalidOperationException(
                $"Failed to start API container. Container logs:\n--- stdout ---\n{stdout}\n--- stderr ---\n{stderr}", ex);
        }

        HttpClient = new HttpClient { BaseAddress = new Uri($"http://localhost:{ApiContainer.GetMappedPublicPort(_containerPort)}") };
        HttpClient.AddBasicApiKey(BasicApiKey, BasicSecret);
    }

    public async Task DisposeAsync()
    {
        HttpClient?.Dispose();
        await ApiContainer.DisposeAsync();
    }
}