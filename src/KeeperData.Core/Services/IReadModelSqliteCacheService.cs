using KeeperData.Core.Storage.Sqlite;

namespace KeeperData.Core.Services;

/// <summary>
/// The locally cached copy of the normalised SAM read model published by the data bridge.
/// </summary>
public interface IReadModelSqliteCacheService
{
    bool IsLoaded { get; }
    DateTime? LastRefreshedAt { get; }
    DateTime? DataTimestamp { get; }
    string? CachedFileName { get; }

    string? GetCurrentDbPath();

    /// <summary>Captures the database path and timestamp from the same cache generation.</summary>
    SqliteSnapshot? GetCurrentSnapshot();

    Task<CacheRefreshResult> ForceRefreshAsync(bool force, CancellationToken cancellationToken = default);
}