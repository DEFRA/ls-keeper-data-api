namespace KeeperData.Core.Messaging.Contracts.V1.Sam;

public class SamHoldingUpdatedMessage : MessageType
{
    public string Identifier { get; set; } = string.Empty;
}
