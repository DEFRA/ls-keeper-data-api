namespace KeeperData.Core.ApiClients.DataBridgeApi.Contracts;

/// <summary>
/// The data bridge response describing the latest published SQLite database.
/// </summary>
public class SqliteArtifactLatestResponse
{
    public string ObjectKey { get; set; } = string.Empty;
    public string DownloadUrl { get; set; } = string.Empty;
    public long Size { get; set; }
    public DateTimeOffset LastModified { get; set; }
    public DateTime ExpiresAt { get; set; }
}