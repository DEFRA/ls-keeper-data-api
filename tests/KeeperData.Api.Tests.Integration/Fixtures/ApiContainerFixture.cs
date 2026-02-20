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

    private readonly bool _enableAnonymization;
    private readonly int _hostPort;
    private readonly int _containerPort;

    public ApiContainerFixture() : this(enableAnonymization: false)
    {
    }

    protected ApiContainerFixture(bool enableAnonymization)
    {
        _enableAnonymization = enableAnonymization;
        _hostPort = enableAnonymization ? 5556 : 5555;
        _containerPort = 5555; // Internal container port stays the same
    }

    public async Task InitializeAsync()
    {
        DockerNetworkHelper.EnsureNetworkExists(NetworkName);

        var containerBuilder = new ContainerBuilder("keeperdata_api:latest")
          .WithName(_enableAnonymization ? "keeperdata_api_anon" : "keeperdata_api")
          .WithPortBinding(_hostPort, _containerPort)
          .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
          .WithEnvironment("ASPNETCORE_HTTP_PORTS", _containerPort.ToString())
          .WithEnvironment("AWS__ServiceURL", _enableAnonymization ? "http://localstack_anon:4566" : "http://localstack:4566")
          .WithEnvironment("Mongo__DatabaseUri",
              $"mongodb://testuser:testpass@{(_enableAnonymization ? "mongo_anon" : "mongo")}:27017/ls-keeper-data-api?authSource=admin")
          .WithEnvironment("StorageConfiguration__ComparisonReportsStorage__BucketName", "test-comparison-reports-bucket")
          .WithEnvironment("QueueConsumerOptions__IntakeEventQueueOptions__QueueUrl", "http://sqs.eu-west-2.127.0.0.1:4566/000000000000/ls_keeper_data_intake_queue")
          .WithEnvironment("QueueConsumerOptions__IntakeEventQueueOptions__DeadLetterQueueUrl", "http://sqs.eu-west-2.127.0.0.1:4566/000000000000/ls_keeper_data_intake_queue-deadletter")
          .WithEnvironment("ApiClients__DataBridgeApi__BaseUrl", "http://localhost:5560/")
          .WithEnvironment("ApiClients__DataBridgeApi__BridgeApiSubscriptionKey", "")
          .WithEnvironment("ApiClients__DataBridgeApi__ServiceName", "")
          .WithEnvironment("ApiClients__DataBridgeApi__XApiKey", "")
          .WithEnvironment("ApiClients__DataBridgeApi__UseFakeClient", "true")
          .WithEnvironment("ServiceBusSenderConfiguration__IntakeEventQueue__QueueUrl", "http://sqs.eu-west-2.127.0.0.1:4566/000000000000/ls_keeper_data_intake_queue")
          .WithEnvironment("BatchCompletionNotificationConfiguration__BatchCompletionEventsTopic__TopicName", "ls_keeper_data_import_complete")
          .WithEnvironment("BatchCompletionNotificationConfiguration__BatchCompletionEventsTopic__TopicArn", "arn:aws:sns:eu-west-2:000000000000:ls_keeper_data_import_complete")
          .WithEnvironment("DataBridgeScanConfiguration__QueryPageSize", "5")
          .WithEnvironment("DataBridgeScanConfiguration__DelayBetweenQueriesSeconds", "0")
          .WithEnvironment("DataBridgeScanConfiguration__LimitScanTotalBatchSize", "10")
          .WithEnvironment("DataBridgeScanConfiguration__DailyScanIncludeChangesWithinTotalHours", "24")
          .WithEnvironment("LOCALSTACK_ENDPOINT", _enableAnonymization ? "http://localstack_anon:4566" : "http://localstack:4566")
          .WithEnvironment("AWS_REGION", "eu-west-2")
          .WithEnvironment("AWS_DEFAULT_REGION", "eu-west-2")
          .WithEnvironment("AWS_ACCESS_KEY_ID", "test")
          .WithEnvironment("AWS_SECRET_ACCESS_KEY", "test");

        containerBuilder = containerBuilder.WithEnvironment("PiiAnonymization__Enabled", _enableAnonymization ? "true" : "false");

        ApiContainer = containerBuilder
              .WithNetwork(NetworkName)
              .WithNetworkAliases(_enableAnonymization ? "keeperdata_api_anon" : "keeperdata_api")
              .WithWaitStrategy(Wait.ForUnixContainer()
                  .UntilHttpRequestIsSucceeded(req => req.ForPort((ushort)_containerPort).ForPath("/health"), o => o.WithTimeout(TimeSpan.FromSeconds(25))))
              .Build();

        await ApiContainer.StartAsync();

        HttpClient = new HttpClient { BaseAddress = new Uri($"http://localhost:{ApiContainer.GetMappedPublicPort(_containerPort)}") };
        HttpClient.AddBasicApiKey(BasicApiKey, BasicSecret);
    }

    public async Task DisposeAsync()
    {
        HttpClient?.Dispose();
        await ApiContainer.DisposeAsync();
    }
}