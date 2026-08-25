namespace KeeperData.Infrastructure.Storage.Configuration;

public record CphSqliteCacheConfiguration : SqliteCacheConfiguration
{
    public const string SectionName = "CphSqliteCache";

    public string FilePattern { get; init; } = "cphs_";
}
