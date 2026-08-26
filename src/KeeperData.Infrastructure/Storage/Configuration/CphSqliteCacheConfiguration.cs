using KeeperData.Core.ApiClients.DataBridgeApi;

namespace KeeperData.Infrastructure.Storage.Configuration;

public record CphSqliteCacheConfiguration : SqliteCacheConfiguration
{
    public const string SectionName = "CphSqliteCache";

    public string FilePattern { get; init; } = "cphs_";

    public string LatestArtifactRoute { get; init; } = DataBridgeApiRoutes.GetLatestCphSqlite;
}
