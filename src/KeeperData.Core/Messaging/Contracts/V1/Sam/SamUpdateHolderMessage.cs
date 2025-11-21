namespace KeeperData.Core.Messaging.Contracts.V1.Sam;

public class SamUpdateHolderMessage : MessageType
{
    public string Identifier { get; set; } = string.Empty;
}