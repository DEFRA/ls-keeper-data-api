using Amazon.Extensions.NETCore.Setup;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using KeeperData.Api.Tests.Component.Consumers.Helpers;
using KeeperData.Core.Messaging.Contracts;
using KeeperData.Core.Messaging.Observers;
using KeeperData.Infrastructure.Storage.Clients;
using KeeperData.Infrastructure.Storage.Factories;
using KeeperData.Infrastructure.Storage.Factories.Implementations;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MongoDB.Driver;
using Moq;
using System.Net;

namespace KeeperData.Api.Tests.Component;

public class AppWebApplicationFactory : WebApplicationFactory<Program>
{
    public Mock<IAmazonS3>? AmazonS3Mock;

    public Mock<IAmazonSQS>? AmazonSQSMock;

    public Mock<IMongoClient>? MongoClientMock;

    public readonly Mock<HttpMessageHandler> DataBridgeApiClientHttpMessageHandlerMock = new();

    private readonly List<Action<IServiceCollection>> _overrideServices = [];

    private const string ComparisonReportsStorageBucket = "test-comparison-reports-bucket";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        SetTestEnvironmentVariables();

        builder.ConfigureTestServices(services =>
        {
            RemoveService<IHealthCheckPublisher>(services);

            ConfigureAwsOptions(services);

            ConfigureS3ClientFactory(services);

            ConfigureSimpleQueueService(services);

            ConfigureDatabase(services);

            services.AddHttpClient("DataBridgeApi")
                .ConfigurePrimaryHttpMessageHandler(() => DataBridgeApiClientHttpMessageHandlerMock.Object);

            foreach (var applyOverride in _overrideServices)
            {
                applyOverride(services);
            }
        });
    }

    protected T GetService<T>() where T : notnull
    {
        return Services.GetRequiredService<T>();
    }

    public void OverrideService<T>(T implementation) where T : class
    {
        _overrideServices.Add(services =>
        {
            services.RemoveAll<T>();
            services.AddSingleton(implementation);
        });
    }

    private static void SetTestEnvironmentVariables()
    {
        Environment.SetEnvironmentVariable("AWS__ServiceURL", "http://localhost:4566");
        Environment.SetEnvironmentVariable("Mongo__DatabaseUri", "mongodb://localhost:27017");
        Environment.SetEnvironmentVariable("StorageConfiguration__ComparisonReportsStorage__BucketName", ComparisonReportsStorageBucket);
        Environment.SetEnvironmentVariable("QueueConsumerOptions__IntakeEventQueueOptions__QueueUrl", "http://localhost:4566/000000000000/test-queue");
        Environment.SetEnvironmentVariable("ApiClients__DataBridgeApi__HealthcheckEnabled", "true");
        Environment.SetEnvironmentVariable("ApiClients__DataBridgeApi__BaseUrl", TestConstants.DataBridgeApiBaseUrl);
    }

    private static void ConfigureAwsOptions(IServiceCollection services)
    {
        var provider = services.BuildServiceProvider();
        var awsOptions = provider.GetRequiredService<AWSOptions>();
        awsOptions.Credentials = new BasicAWSCredentials("test", "test");
        services.Replace(new ServiceDescriptor(typeof(AWSOptions), awsOptions));
    }

    private void ConfigureS3ClientFactory(IServiceCollection services)
    {
        AmazonS3Mock = new Mock<IAmazonS3>();

        AmazonS3Mock
            .Setup(x => x.GetBucketAclAsync(It.IsAny<GetBucketAclRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetBucketAclResponse { HttpStatusCode = HttpStatusCode.OK });

        AmazonS3Mock
            .Setup(x => x.ListObjectsV2Async(It.IsAny<ListObjectsV2Request>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ListObjectsV2Response { HttpStatusCode = HttpStatusCode.OK });

        var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IS3ClientFactory>();

        if (factory is S3ClientFactory concreteFactory)
        {
            concreteFactory.RegisterMockClient<ComparisonReportsStorageClient>(ComparisonReportsStorageBucket, AmazonS3Mock.Object);
        }
    }

    private void ConfigureSimpleQueueService(IServiceCollection services)
    {
        services.RemoveAll<IAmazonSQS>();

        AmazonSQSMock = new Mock<IAmazonSQS>();

        AmazonSQSMock
            .Setup(x => x.GetQueueAttributesAsync(It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetQueueAttributesResponse { HttpStatusCode = HttpStatusCode.OK });

        services.AddSingleton(AmazonSQSMock.Object);

        services.AddSingleton<TestQueuePollerObserver<MessageType>>();
        services.AddScoped<IQueuePollerObserver<MessageType>>(sp => sp.GetRequiredService<TestQueuePollerObserver<MessageType>>());
    }

    private void ConfigureDatabase(IServiceCollection services)
    {
        MongoClientMock = new Mock<IMongoClient>();

        MongoClientMock.Setup(x => x.GetDatabase(It.IsAny<string>(), It.IsAny<MongoDatabaseSettings>()))
            .Returns(() => new Mock<IMongoDatabase>().Object);

        services.Replace(new ServiceDescriptor(typeof(IMongoClient), MongoClientMock.Object));
    }

    private static void RemoveService<T>(IServiceCollection services)
    {
        var service = services.FirstOrDefault(x => x.ServiceType == typeof(T));
        if (service != null)
        {
            services.Remove(service);
        }
    }
}