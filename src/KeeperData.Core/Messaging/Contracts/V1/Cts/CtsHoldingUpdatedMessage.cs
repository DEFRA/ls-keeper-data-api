namespace KeeperData.Core.Messaging.Contracts.V1.Cts;

public class CtsHoldingUpdatedMessage : MessageType
{
    public string Identifier { get; set; } = string.Empty;
}
