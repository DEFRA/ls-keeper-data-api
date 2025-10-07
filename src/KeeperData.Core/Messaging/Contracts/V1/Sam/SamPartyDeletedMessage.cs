namespace KeeperData.Core.Messaging.Contracts.V1.Sam;

public class SamPartyDeletedMessage : MessageType
{
    public string Identifier { get; set; } = string.Empty;
}
