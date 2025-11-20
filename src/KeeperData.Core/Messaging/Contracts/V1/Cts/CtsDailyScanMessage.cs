namespace KeeperData.Core.Messaging.Contracts.V1.Cts;

public class CtsDailyScanMessage : MessageType
{
    public string Identifier { get; set; } = string.Empty;
}