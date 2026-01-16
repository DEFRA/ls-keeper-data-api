using KeeperData.Core.Messaging.Throttling;

namespace KeeperData.Infrastructure.Messaging.Throttling;

public class DataImportThrottlingConfiguration : IDataImportThrottlingConfiguration
{
    public const string SectionName = "DataImportThrottlingSettings";

    public int MessageCompletionDelayMs { get; set; }
}