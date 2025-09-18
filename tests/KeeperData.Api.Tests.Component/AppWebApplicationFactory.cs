using Amazon.Extensions.NETCore.Setup;
using Amazon.Runtime;
using Amazon.SQS;
using Amazon.SQS.Model;
using KeeperData.Api.Tests.Component.Consumers.Helpers;
using KeeperData.Core.Messaging.Contracts;
using KeeperData.Core.Messaging.Observers;
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
    public Mock<IAmazonSQS>? AmazonSQSMock;

    public Mock<IMongoClient>? MongoClientMock;

    private readonly List<Action<IServiceCollection>> _overrideServices = [];

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        SetTestEnvironmentVariables();

        builder.ConfigureTestServices(services =>
        {
            RemoveService<IHealthCheckPublisher>(services);

            ConfigureAwsOptions(services);

            ConfigureSimpleQueueService(services);

            ConfigureDatabase(services);

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
        Environment.SetEnvironmentVariable("QueueConsumerOptions__IntakeEventQueueOptions__QueueUrl", "http://localhost:4566/000000000000/test-queue");
    }

    private static void ConfigureAwsOptions(IServiceCollection services)
    {
        var provider = services.BuildServiceProvider();
        var awsOptions = provider.GetRequiredService<AWSOptions>();
        awsOptions.Credentials = new BasicAWSCredentials("test", "test");
        services.Replace(new ServiceDescriptor(typeof(AWSOptions), awsOptions));
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