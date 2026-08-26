using KeeperData.Core.ApiClients.DataBridgeApi;

namespace KeeperData.Infrastructure.Storage.Configuration;

/// <summary>
/// Settings for the locally cached copy of the normalised SAM read model published by the data
/// bridge as views/krds-db_yyyyMMddHHmmss.sqlite.
/// </summary>
public record ReadModelSqliteCacheConfiguration : SqliteCacheConfiguration
{
    public const string SectionName = "ReadModelSqliteCache";

    public string FilePattern { get; init; } = "krds-db_";

    public string LatestArtifactRoute { get; init; } = DataBridgeApiRoutes.GetLatestSqliteReadModel;
}
