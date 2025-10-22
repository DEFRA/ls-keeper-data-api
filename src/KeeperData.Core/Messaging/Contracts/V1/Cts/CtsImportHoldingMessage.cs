namespace KeeperData.Core.Messaging.Contracts.V1.Cts;

public class CtsImportHoldingMessage : MessageType
{
    public string Identifier { get; set; } = string.Empty;
}