using KeeperData.Core.Services;
using KeeperData.Core.Storage.Sqlite;
using KeeperData.Infrastructure.Storage.Configuration;
using Microsoft.Extensions.Logging;

namespace KeeperData.Infrastructure.Services;

public class CphSqliteCacheService : SqliteCacheService, ICphSqliteCacheService
{
    private readonly CphSqliteCacheConfiguration _config;

    public CphSqliteCacheService(
        ISqliteArtifactSource artifactSource,
        CphSqliteCacheConfiguration config,
        ILogger<CphSqliteCacheService> logger)
        : base(artifactSource, config, logger)
    {
        _config = config;
    }

    protected override string LatestArtifactRoute => _config.LatestArtifactRoute;

    protected override string FilePattern => _config.FilePattern;

    protected override string TimestampFormat => "yyyyMMdd'T'HHmmss'Z'";

    protected override string RowCountSql => "SELECT COUNT(*) FROM cphs";

    protected override string CacheName => "CPH";
}
