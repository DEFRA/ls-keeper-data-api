namespace KeeperData.Core.Storage.Sqlite;

/// <summary>A database path and its data timestamp, published together when a cache refresh completes.</summary>
public sealed record SqliteSnapshot(string DbPath, DateTime? DataTimestamp);