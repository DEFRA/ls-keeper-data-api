namespace KeeperData.Core.ApiClients.DataBridgeApi.Contracts;

public class BronzeBase
{
    public required int BATCH_ID { get; set; }
    public required string CHANGE_TYPE { get; set; }
}