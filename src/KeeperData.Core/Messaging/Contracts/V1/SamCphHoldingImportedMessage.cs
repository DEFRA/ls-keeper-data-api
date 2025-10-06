namespace KeeperData.Core.Messaging.Contracts.V1;

public class SamCphHoldingImportedMessage : MessageType
{
    public string Identifier { get; set; } = string.Empty;
}