namespace KeeperData.Infrastructure.Storage.Configuration;

public record StorageConfiguration
{
    public StorageConfigurationDetails ComparisonReportsStorage { get; init; } = new();
    public StorageConfigurationDetails CphSqliteStorage { get; init; } = new();
}