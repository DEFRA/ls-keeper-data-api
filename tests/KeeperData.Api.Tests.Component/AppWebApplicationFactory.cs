using Amazon.Extensions.NETCore.Setup;
using Amazon.Runtime;
using Amazon.SQS;
using Amazon.SQS.Model;
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
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        SetTestEnvironmentVariables();

        builder.ConfigureTestServices(services =>
        {
            RemoveService<IHealthCheckPublisher>(services);

            ConfigureAwsOptions(services);

            ConfigureSimpleQueueService(services);

            ConfigureDatabase(services);
        });
    }

    protected T GetService<T>() where T : notnull
    {
        return Services.GetRequiredService<T>();
    }

    private static void SetTestEnvironmentVariables()
    {
        Environment.SetEnvironmentVariable("AWS__ServiceURL", "http://localhost:4566");
        Environment.SetEnvironmentVariable("Mongo__DatabaseUri", "mongodb://localhost:27017");
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
        RemoveService<IAmazonSQS>(services);

        AmazonSQSMock = new Mock<IAmazonSQS>();

        AmazonSQSMock
            .Setup(x => x.GetQueueAttributesAsync(It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetQueueAttributesResponse { HttpStatusCode = HttpStatusCode.OK });

        services.Replace(new ServiceDescriptor(typeof(IAmazonSQS), AmazonSQSMock.Object));
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