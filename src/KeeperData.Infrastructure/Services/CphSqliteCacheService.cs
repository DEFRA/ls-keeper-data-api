using KeeperData.Core.Services;
using KeeperData.Infrastructure.Storage.Clients;
using KeeperData.Infrastructure.Storage.Configuration;
using KeeperData.Infrastructure.Storage.Factories;
using Microsoft.Extensions.Logging;

namespace KeeperData.Infrastructure.Services;

public class CphSqliteCacheService : S3SqliteCacheService<CphSqliteStorageClient>, ICphSqliteCacheService
{
    private readonly CphSqliteCacheConfiguration _config;

    public CphSqliteCacheService(
        IS3ClientFactory s3ClientFactory,
        CphSqliteCacheConfiguration config,
        ILogger<CphSqliteCacheService> logger)
        : base(s3ClientFactory, config, logger)
    {
        _config = config;
    }

    protected override string FilePattern => _config.FilePattern;

    protected override string TimestampFormat => "yyyyMMdd'T'HHmmss'Z'";

    protected override string RowCountSql => "SELECT COUNT(*) FROM cphs";

    protected override string CacheName => "CPH";
}
