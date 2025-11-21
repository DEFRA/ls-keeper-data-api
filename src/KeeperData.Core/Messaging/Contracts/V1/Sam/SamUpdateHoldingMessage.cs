namespace KeeperData.Core.Messaging.Contracts.V1.Sam;

public class SamUpdateHoldingMessage : MessageType
{
    public string Identifier { get; set; } = string.Empty;
}