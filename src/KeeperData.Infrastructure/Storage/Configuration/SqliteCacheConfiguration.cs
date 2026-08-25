namespace KeeperData.Infrastructure.Storage.Configuration;

/// <summary>
/// Settings shared by every locally cached SQLite database published to the views folder.
/// </summary>
public abstract record SqliteCacheConfiguration
{
    public bool Enabled { get; init; } = true;
    public string CachePath { get; init; } = "data/cache";
    public string S3Prefix { get; init; } = "views/";
    public int RefreshIntervalHours { get; init; } = 24;
    public int CleanupDelayMs { get; init; } = 5000;
}
