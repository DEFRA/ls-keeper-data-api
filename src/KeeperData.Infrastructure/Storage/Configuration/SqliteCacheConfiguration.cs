namespace KeeperData.Infrastructure.Storage.Configuration;

/// <summary>
/// Settings shared by every locally cached SQLite database published by the data bridge.
/// </summary>
public abstract record SqliteCacheConfiguration
{
    public bool Enabled { get; init; } = true;
    public string CachePath { get; init; } = "data/cache";
    public int RefreshIntervalHours { get; init; } = 24;
    public int CleanupDelayMs { get; init; } = 5000;
}