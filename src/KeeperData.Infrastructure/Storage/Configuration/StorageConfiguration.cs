namespace KeeperData.Infrastructure.Storage.Configuration;

public record StorageConfiguration
{
    public StorageConfigurationDetails ComparisonReportsStorage { get; init; } = new();
}