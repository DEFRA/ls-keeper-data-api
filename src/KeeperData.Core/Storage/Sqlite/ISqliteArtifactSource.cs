namespace KeeperData.Core.Storage.Sqlite;

/// <summary>
/// Supplies the SQLite databases the data bridge publishes to its own storage. The bridge owns the
/// bucket and decides which file is current, so this service only asks for the latest artifact and
/// downloads it.
/// </summary>
public interface ISqliteArtifactSource
{
    /// <summary>Returns the latest artifact for a route, or null when the bridge holds none.</summary>
    Task<SqliteArtifact?> GetLatestAsync(string latestArtifactRoute, CancellationToken cancellationToken);

    Task DownloadAsync(SqliteArtifact artifact, string localPath, CancellationToken cancellationToken);
}
