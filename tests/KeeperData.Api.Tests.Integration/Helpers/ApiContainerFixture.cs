namespace KeeperData.Api.Tests.Integration.Helpers;

using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using System.Net.Http;
using System;
using System.Threading.Tasks;
using Xunit;

[Collection("Integration"), Trait("Dependence", "testcontainers")]
public class ApiContainerFixture : IAsyncLifetime
{
    public IContainer ApiContainer { get; private set; } = null!;
    public HttpClient HttpClient { get; private set; } = null!;

    private readonly string _mongoConnectionString;
    private readonly string _localStackEndpoint;

    public ApiContainerFixture(string mongoConnectionString, string localStackEndpoint)
    {
        _mongoConnectionString = mongoConnectionString;
        _localStackEndpoint = localStackEndpoint;
    }

    public async Task InitializeAsync()
    {
        ApiContainer = new ContainerBuilder()
            .WithImage("keeperdata_api:latest")
            .WithPortBinding(5555, 5555)
            .WithEnvironment("MONGO_CONNECTION_STRING", _mongoConnectionString)
            .WithEnvironment("LOCALSTACK_ENDPOINT", _localStackEndpoint)
            .WithEnvironment("AWS__Region", "eu-west-2")
            .WithEnvironment("AWS_ACCESS_KEY_ID", "test")
            .WithEnvironment("AWS_SECRET_ACCESS_KEY", "test")
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilHttpRequestIsSucceeded(req => req.ForPath("/health").ForPort(5555)))
            .Build();

        await ApiContainer.StartAsync();

        var mappedPort = ApiContainer.GetMappedPublicPort(5555);
        HttpClient = new HttpClient
        {
            BaseAddress = new Uri($"http://localhost:{mappedPort}"),
            Timeout = TimeSpan.FromSeconds(30)
        };
        HttpClient.DefaultRequestHeaders.Add("Accept", "application/json");
    }

    public async Task DisposeAsync()
    {
        HttpClient?.Dispose();
        await ApiContainer.DisposeAsync();
    }
}

