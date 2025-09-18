using KeeperData.Core.Messaging.Contracts;

namespace KeeperData.Api.Tests.Integration.Consumers.Contracts;

public class IntegrationTestMessage : MessageType
{
    public string Message { get; init; } = string.Empty;
}
