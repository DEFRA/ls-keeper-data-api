using KeeperData.Api.Tests.Integration.TestUtils;

namespace KeeperData.Api.Tests.Integration.Consumers;

public class IntakeEventConsumerTests : IntegrationTestBase
{
    [Fact]
    public async Task MessagePublishToQueue_ShouldBeConsumed()
    {
        // Arrange
        var message = "Hello World";
        var payload = $"{{ \"Message\": \"{message}\" }}";

        // Act
        await PublishMessageAsync(payload);

        // Assert
        // await WebAppFactory!.IntakeEventRepositoryMock.Received(1).CreateAsync(Arg.Is<IntakeEventModel>(x => x.Message == message), Arg.Any<CancellationToken>());
    }
}