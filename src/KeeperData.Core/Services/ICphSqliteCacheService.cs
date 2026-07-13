namespace KeeperData.Core.Services;

public interface ICphSqliteCacheService
{
    bool IsLoaded { get; }
    DateTime? LastRefreshedAt { get; }
    DateTime? DataTimestamp { get; }
    string? CachedFileName { get; }

    string? GetCurrentDbPath();
}