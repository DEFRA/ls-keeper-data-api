using KeeperData.Core.Messaging.Throttling;

namespace KeeperData.Infrastructure.Messaging.Throttling;

public class DataImportThrottlingConfiguration : IDataImportThrottlingConfiguration
{
    public int MessageCompletionDelayMs { get; set; }
}