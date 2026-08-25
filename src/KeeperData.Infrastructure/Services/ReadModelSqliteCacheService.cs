using KeeperData.Core.Services;
using KeeperData.Infrastructure.Storage.Clients;
using KeeperData.Infrastructure.Storage.Configuration;
using KeeperData.Infrastructure.Storage.Factories;
using Microsoft.Extensions.Logging;

namespace KeeperData.Infrastructure.Services;

/// <summary>
/// Caches the normalised SAM read model (Party, Holding, PartyRole, Herd, HoldingAnimalProfile)
/// published by the data bridge, which is the source of a user's current CPH associations.
/// </summary>
public class ReadModelSqliteCacheService : S3SqliteCacheService<CphSqliteStorageClient>, IReadModelSqliteCacheService
{
    private readonly ReadModelSqliteCacheConfiguration _config;

    public ReadModelSqliteCacheService(
        IS3ClientFactory s3ClientFactory,
        ReadModelSqliteCacheConfiguration config,
        ILogger<ReadModelSqliteCacheService> logger)
        : base(s3ClientFactory, config, logger)
    {
        _config = config;
    }

    protected override string FilePattern => _config.FilePattern;

    protected override string TimestampFormat => "yyyyMMddHHmmss";

    protected override string RowCountSql => "SELECT COUNT(*) FROM Party";

    protected override string CacheName => "Read model";
}
