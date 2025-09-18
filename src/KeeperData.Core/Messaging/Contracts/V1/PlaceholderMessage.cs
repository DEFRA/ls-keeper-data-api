namespace KeeperData.Core.Messaging.Contracts.V1;

public class PlaceholderMessage : MessageType
{
    public string Message { get; init; } = string.Empty;
}