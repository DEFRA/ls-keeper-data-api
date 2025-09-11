using KeeperData.Api.Tests.Integration.TestUtils;
using KeeperData.Core.Models;
using NSubstitute;

namespace KeeperData.Api.Tests.Integration.QueueConsumers;

public class IntakeEventConsumerTests : IntegrationTestBase
{
    [Fact]
    public async Task MessagePublishToQueue_ShouldBeConsumed()
    {
        // Arrange
        var message = "Hello World";
        var payload = $"{{ \"Message\": \"{message}\" }}";

        // Act
        await this.PublishMessageAsync(payload, "http://sqs.eu-west-2.127.0.0.1:4566/000000000000/ls_keeper_data_intake_queue");

        // Assert
        await WebAppFactory!.IntakeEventRepositoryMock.Received(1).CreateAsync(Arg.Is<IntakeEventModel>(x => x.Message == message), Arg.Any<CancellationToken>());
    }
}