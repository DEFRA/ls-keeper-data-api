using FluentAssertions;
using KeeperData.Infrastructure.Messaging.Publishers.Clients;

namespace KeeperData.Infrastructure.Tests.Unit.Messaging.MessagePublishers;

public class IntakeEventsTopicClientTests
{
    [Fact]
    public void ClientName_ReturnsClassName()
    {
        var client = new IntakeEventsTopicClient();

        var result = client.ClientName;

        result.Should().Be(nameof(IntakeEventsTopicClient));
    }
}