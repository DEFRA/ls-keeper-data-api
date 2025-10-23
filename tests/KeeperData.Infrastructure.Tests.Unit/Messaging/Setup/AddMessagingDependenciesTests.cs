using Amazon.Extensions.NETCore.Setup;
using Amazon.Runtime;
using Amazon.SimpleNotificationService;
using Amazon.SQS;
using FluentAssertions;
using KeeperData.Infrastructure.Messaging.Configuration;
using KeeperData.Infrastructure.Messaging.Publishers.Configuration;
using KeeperData.Infrastructure.Messaging.Setup;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KeeperData.Infrastructure.Tests.Unit.Messaging.Setup;

public class AddMessagingDependenciesTests
{
    [Fact]
    public void AddMessagingDependencies_ShouldRegisterOptionsAndAwsClient_WhenLocalstackIsNotSet()
    {
        // Arrange
        var configData = new Dictionary<string, string?>
        {
            ["QueueConsumerOptions:IntakeEventQueueOptions:QueueUrl"] = "http://localhost:4566/000000000000/test-queue",
            ["QueueConsumerOptions:IntakeEventQueueOptions:WaitTimeSeconds"] = "5",
            ["QueueConsumerOptions:IntakeEventQueueOptions:MaxNumberOfMessages"] = "10",
            ["ServiceBusSenderConfiguration:IntakeEventsTopic:TopicName"] = "ls-keeper-data-bridge-events",
            ["ServiceBusSenderConfiguration:IntakeEventsTopic:TopicArn"] = "arn:aws:sns:eu-west-2:000000000000:ls-keeper-data-bridge-events"
        };

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        var services = new ServiceCollection();

        // Act
        services.AddMessagingDependencies(config);
        services.AddDefaultAWSOptions(new AWSOptions
        {
            Region = Amazon.RegionEndpoint.EUWest2,
            Credentials = new BasicAWSCredentials("test", "test")
        });

        var provider = services.BuildServiceProvider();

        // Assert
        var intakeEventQueueOptions = provider.GetRequiredService<IntakeEventQueueOptions>();
        intakeEventQueueOptions.QueueUrl.Should().Be("http://localhost:4566/000000000000/test-queue");
        intakeEventQueueOptions.WaitTimeSeconds.Should().Be(5);
        intakeEventQueueOptions.MaxNumberOfMessages.Should().Be(10);

        var serviceBusSenderConfiguration = provider.GetRequiredService<IServiceBusSenderConfiguration>();
        serviceBusSenderConfiguration.IntakeEventsTopic.TopicName.Should().Be("ls-keeper-data-bridge-events");
        serviceBusSenderConfiguration.IntakeEventsTopic.TopicArn.Should().Be("arn:aws:sns:eu-west-2:000000000000:ls-keeper-data-bridge-events");

        provider.GetRequiredService<IAmazonSQS>().Should().NotBeNull();
        provider.GetRequiredService<IAmazonSimpleNotificationService>().Should().NotBeNull();
    }

    [Fact]
    public void AddMessagingDependencies_ShouldRegisterLocalstackClient_WhenLocalstackIsSet()
    {
        // Arrange
        var configData = new Dictionary<string, string?>
        {
            ["LOCALSTACK_ENDPOINT"] = "true",
            ["AWS:ServiceURL"] = "http://localhost:4566/",
            ["AWS:Region"] = "eu-west-2",
            ["QueueConsumerOptions:IntakeEventQueueOptions:QueueUrl"] = "http://localhost:4566/000000000000/test-queue",
            ["ServiceBusSenderConfiguration:IntakeEventsTopic:TopicName"] = "ls-keeper-data-bridge-events",
            ["ServiceBusSenderConfiguration:IntakeEventsTopic:TopicArn"] = "arn:aws:sns:eu-west-2:000000000000:ls-keeper-data-bridge-events"
        };

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        var services = new ServiceCollection();

        // Act
        services.AddMessagingDependencies(config);
        services.AddDefaultAWSOptions(new AWSOptions
        {
            Region = Amazon.RegionEndpoint.EUWest2,
            Credentials = new BasicAWSCredentials("test", "test")
        });

        var provider = services.BuildServiceProvider();

        // Assert
        var sqsClient = provider.GetRequiredService<IAmazonSQS>();
        sqsClient.Should().NotBeNull();
        sqsClient.Config.ServiceURL.Should().Be("http://localhost:4566/");
        sqsClient.Config.AuthenticationRegion.Should().Be("eu-west-2");

        var snsClient = provider.GetRequiredService<IAmazonSimpleNotificationService>();
        snsClient.Should().NotBeNull();
        snsClient.Config.ServiceURL.Should().Be("http://localhost:4566/");
        snsClient.Config.AuthenticationRegion.Should().Be("eu-west-2");
    }
}