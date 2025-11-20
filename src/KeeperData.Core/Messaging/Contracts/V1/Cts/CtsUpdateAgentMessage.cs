namespace KeeperData.Core.Messaging.Contracts.V1.Cts;

public class CtsUpdateAgentMessage : MessageType
{
    public string Identifier { get; set; } = string.Empty;
}