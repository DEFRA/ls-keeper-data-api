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
}