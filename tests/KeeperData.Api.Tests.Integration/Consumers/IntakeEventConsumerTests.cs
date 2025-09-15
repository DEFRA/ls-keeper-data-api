using FluentAssertions;
using KeeperData.Api.Tests.Integration.Consumers.Helpers;
using KeeperData.Api.Tests.Integration.TestUtils;
using KeeperData.Core.Messaging.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace KeeperData.Api.Tests.Integration.Consumers;

public class IntakeEventConsumerTests : IntegrationTestBase
{
    private TestConsumerObserver<IntakeEventModel>? _observer;

    [Fact]
    public async Task MessagePublishToQueue_ShouldBeConsumed()
    {
        // Arrange
        var message = "Hello World";
        var payload = $"{{ \"Message\": \"{message}\" }}";

        using var scope = WebAppFactory!.Server.Services.CreateAsyncScope();
        _observer = scope.ServiceProvider.GetRequiredService<TestConsumerObserver<IntakeEventModel>>();

        // Act
        await PublishMessageAsync(payload);

        // Assert
        var (_, Payload) = await _observer!.MessageHandled;
        Payload.Message.Should().NotBeNull().And.Be(message);
    }
}