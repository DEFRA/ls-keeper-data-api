using Amazon.SQS;
using FluentAssertions;
using KeeperData.Infrastructure.Messaging.Configuration;
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
            ["QueueConsumerOptions:IntakeEventQueueOptions:MaxNumberOfMessages"] = "10"
        };

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        var services = new ServiceCollection();

        // Act
        services.AddMessagingDependencies(config);
        var provider = services.BuildServiceProvider();

        // Assert
        var options = provider.GetRequiredService<IntakeEventQueueOptions>();
        options.QueueUrl.Should().Be("http://localhost:4566/000000000000/test-queue");
        options.WaitTimeSeconds.Should().Be(5);
        options.MaxNumberOfMessages.Should().Be(10);

        provider.GetRequiredService<IAmazonSQS>().Should().NotBeNull();
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
            ["QueueConsumerOptions:IntakeEventQueueOptions:QueueUrl"] = "http://localhost:4566/000000000000/test-queue"
        };

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        var services = new ServiceCollection();

        // Act
        services.AddMessagingDependencies(config);
        var provider = services.BuildServiceProvider();

        // Assert
        var sqsClient = provider.GetRequiredService<IAmazonSQS>();
        sqsClient.Should().NotBeNull();
        sqsClient.Config.ServiceURL.Should().Be("http://localhost:4566/");
        sqsClient.Config.AuthenticationRegion.Should().Be("eu-west-2");
    }
}