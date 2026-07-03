namespace KeeperData.Infrastructure.Storage.Configuration;

public record CphSqliteCacheConfiguration
{
    public const string SectionName = "CphSqliteCache";

    public bool Enabled { get; init; } = true;
    public string CachePath { get; init; } = "data/cache";
    public string S3Prefix { get; init; } = "views/";
    public string FilePattern { get; init; } = "cphs_";
    public int RefreshIntervalHours { get; init; } = 24;
}
