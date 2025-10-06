namespace KeeperData.Core.Messaging.Contracts.V1;

public class CtsCphHoldingImportedMessage : MessageType
{
    public string Identifier { get; set; } = string.Empty;
}