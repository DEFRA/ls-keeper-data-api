namespace KeeperData.Core.Messaging.Throttling;

public interface IDataImportThrottlingConfiguration
{
    int MessageCompletionDelayMs { get; set; }
}