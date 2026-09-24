namespace KeeperData.Core.Services;

public sealed record CacheRefreshResult(
    string Cache,
    string Status,
    string? FileName,
    long? RowCount,
    DateTime? DataTimestamp,
    long ElapsedMs,
    string? Error = null);