using KeeperData.Core.Services;
using KeeperData.Core.Storage.Sqlite;
using KeeperData.Infrastructure.Storage.Configuration;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace KeeperData.Infrastructure.Services;

/// <summary>
/// Caches the normalised SAM read model (Party, Holding, PartyRole, Herd, HoldingAnimalProfile)
/// published by the data bridge, which is the source of a user's current CPH associations.
/// </summary>
public class ReadModelSqliteCacheService : SqliteCacheService, IReadModelSqliteCacheService
{
    private readonly ReadModelSqliteCacheConfiguration _config;
    private readonly ILogger<ReadModelSqliteCacheService> _logger;

    public ReadModelSqliteCacheService(
        ISqliteArtifactSource artifactSource,
        ReadModelSqliteCacheConfiguration config,
        ILogger<ReadModelSqliteCacheService> logger)
        : base(artifactSource, config, logger)
    {
        _config = config;
        _logger = logger;
    }

    protected override string LatestArtifactRoute => _config.LatestArtifactRoute;

    protected override string FilePattern => _config.FilePattern;

    protected override string TimestampFormat => "yyyyMMddHHmmss";

    protected override string RowCountSql => "SELECT COUNT(*) FROM Party";

    protected override string CacheName => "Read model";

    protected override bool RequiresSearchIndex => true;

    protected override async Task<string?> BuildSearchIndexAsync(string dbPath, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var indexPath = await HoldingSearchIndex.BuildAsync(dbPath, cancellationToken);
            _logger.LogInformation("Holding search index created in {DurationMs}ms with {DocumentCount} documents",
                stopwatch.ElapsedMilliseconds, indexPath.DocumentCount);
            return indexPath.Path;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Holding search index creation failed after {DurationMs}ms", stopwatch.ElapsedMilliseconds);
            if (GetCurrentSnapshot() is not null)
                throw;

            // The validated read model can still serve requests that do not search.
            return null;
        }
    }
}