namespace KeeperData.Core.Messaging.Contracts.V1;

public class CphHoldingImportedMessage : MessageType
{
    public string Identifier { get; set; } = string.Empty;
}