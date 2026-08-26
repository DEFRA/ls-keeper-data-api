using KeeperData.Core.Services;
using KeeperData.Core.Storage.Sqlite;
using KeeperData.Infrastructure.Storage.Configuration;
using Microsoft.Extensions.Logging;

namespace KeeperData.Infrastructure.Services;

/// <summary>
/// Caches the normalised SAM read model (Party, Holding, PartyRole, Herd, HoldingAnimalProfile)
/// published by the data bridge, which is the source of a user's current CPH associations.
/// </summary>
public class ReadModelSqliteCacheService : SqliteCacheService, IReadModelSqliteCacheService
{
    private readonly ReadModelSqliteCacheConfiguration _config;

    public ReadModelSqliteCacheService(
        ISqliteArtifactSource artifactSource,
        ReadModelSqliteCacheConfiguration config,
        ILogger<ReadModelSqliteCacheService> logger)
        : base(artifactSource, config, logger)
    {
        _config = config;
    }

    protected override string LatestArtifactRoute => _config.LatestArtifactRoute;

    protected override string FilePattern => _config.FilePattern;

    protected override string TimestampFormat => "yyyyMMddHHmmss";

    protected override string RowCountSql => "SELECT COUNT(*) FROM Party";

    protected override string CacheName => "Read model";
}