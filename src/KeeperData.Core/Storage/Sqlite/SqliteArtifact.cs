namespace KeeperData.Core.Storage.Sqlite;

/// <summary>
/// A SQLite database published by the data bridge, together with a short-lived download URL. The URL
/// is presigned and usable by anyone holding it, so it is never logged.
/// </summary>
public sealed record SqliteArtifact
{
    public required string ObjectKey { get; init; }
    public required string DownloadUrl { get; init; }
    public long Size { get; init; }
    public DateTimeOffset LastModified { get; init; }
    public DateTime ExpiresAt { get; init; }

    public string FileName => ObjectKey.Split('/').Last();
}
