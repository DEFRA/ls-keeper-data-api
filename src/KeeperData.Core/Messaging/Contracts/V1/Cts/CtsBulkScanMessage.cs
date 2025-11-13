namespace KeeperData.Core.Messaging.Contracts.V1.Cts;

public class CtsBulkScanMessage : MessageType
{
    public string Identifier { get; set; } = string.Empty;
}