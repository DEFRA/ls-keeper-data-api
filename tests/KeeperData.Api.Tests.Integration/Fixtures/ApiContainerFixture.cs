using KeeperData.Api.Tests.Integration.Helpers;

namespace KeeperData.Api.Tests.Integration.Fixtures;

using DotNet.Testcontainers.Builders;
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

    public async Task InitializeAsync()
    {
        DockerNetworkHelper.EnsureNetworkExists(NetworkName);

        ApiContainer = new ContainerBuilder("keeperdata_api:latest")
          .WithName("keeperdata_api")
          .WithPortBinding(5555, 5555)
          .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
          .WithEnvironment("ASPNETCORE_HTTP_PORTS", "5555")
          .WithEnvironment("AWS__ServiceURL", "http://localstack:4566")
          .WithEnvironment("Mongo__DatabaseUri", "mongodb://testuser:testpass@mongo:27017/ls-keeper-data-api?authSource=admin")
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
          .WithEnvironment("LOCALSTACK_ENDPOINT", "http://localstack:4566")
          .WithEnvironment("AWS_REGION", "eu-west-2")
          .WithEnvironment("AWS_DEFAULT_REGION", "eu-west-2")
          .WithEnvironment("AWS_ACCESS_KEY_ID", "test")
          .WithEnvironment("AWS_SECRET_ACCESS_KEY", "test")
          .WithEnvironment("MongoDbPreproductionService__Enabled", "true")
          .WithNetwork(NetworkName)
          .WithNetworkAliases("keeperdata_api")
          .WithWaitStrategy(Wait.ForUnixContainer()
              .UntilHttpRequestIsSucceeded(req => req.ForPort(5555).ForPath("/health"), o => o.WithTimeout(TimeSpan.FromSeconds(25))))
          .Build();

        await ApiContainer.StartAsync();

        HttpClient = new HttpClient { BaseAddress = new Uri($"http://localhost:{ApiContainer.GetMappedPublicPort(5555)}") };
    }

    public async Task DisposeAsync()
    {
        HttpClient?.Dispose();
        await ApiContainer.DisposeAsync();
    }
}