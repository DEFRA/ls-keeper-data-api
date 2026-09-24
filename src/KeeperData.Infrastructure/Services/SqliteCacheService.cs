using KeeperData.Core.Services;
using KeeperData.Core.Storage.Sqlite;
using KeeperData.Infrastructure.Storage.Configuration;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Diagnostics;

namespace KeeperData.Infrastructure.Services;

/// <summary>
/// Keeps a local copy of the most recent SQLite database the data bridge publishes, replacing it in
/// place as newer files appear. A refresh failure leaves the previously cached file serving reads.
/// </summary>
public abstract class SqliteCacheService : IHostedService, IDisposable
{
    private readonly ISqliteArtifactSource _artifactSource;
    private readonly SqliteCacheConfiguration _config;
    private readonly ILogger _logger;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    private volatile SqliteSnapshot? _currentSnapshot;
    private volatile bool _isLoaded;
    private volatile string? _cachedFileName;
    private DateTime? _lastRefreshedAt;
    private long? _rowCount;
    private Timer? _refreshTimer;
    private bool _disposed;

    public bool IsLoaded => _isLoaded;
    public DateTime? LastRefreshedAt => _lastRefreshedAt;
    public DateTime? DataTimestamp => _currentSnapshot?.DataTimestamp;
    public string? CachedFileName => _cachedFileName;

    protected SqliteCacheService(
        ISqliteArtifactSource artifactSource,
        SqliteCacheConfiguration config,
        ILogger logger)
    {
        _artifactSource = artifactSource;
        _config = config;
        _logger = logger;
    }

    /// <summary>The data bridge route serving the latest copy of this database.</summary>
    protected abstract string LatestArtifactRoute { get; }

    /// <summary>The file name prefix identifying the database this cache serves, e.g. "cphs_".</summary>
    protected abstract string FilePattern { get; }

    /// <summary>The format of the timestamp carried in the file name, e.g. "yyyyMMddHHmmss".</summary>
    protected abstract string TimestampFormat { get; }

    /// <summary>A statement proving the downloaded file holds the expected schema.</summary>
    protected abstract string RowCountSql { get; }

    /// <summary>Names the cache in log messages.</summary>
    protected abstract string CacheName { get; }

    public string? GetCurrentDbPath() => _currentSnapshot?.DbPath;

    public SqliteSnapshot? GetCurrentSnapshot() => _currentSnapshot;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_config.Enabled)
        {
            _logger.LogInformation("{CacheName} SQLite cache is disabled", CacheName);
            return;
        }

        Directory.CreateDirectory(_config.CachePath);

        await RefreshCacheAsync(cancellationToken);

        var intervalMs = _config.RefreshIntervalHours * 3600 * 1000;
        _refreshTimer = new Timer(
            async _ => await RefreshCacheAsync(CancellationToken.None),
            null,
            intervalMs,
            intervalMs);

        _logger.LogInformation(
            "{CacheName} SQLite cache refresh timer started with interval {IntervalHours}h",
            CacheName, _config.RefreshIntervalHours);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _refreshTimer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    internal async Task RefreshCacheAsync(CancellationToken cancellationToken)
    {
        await RefreshAsync(false, cancellationToken);
    }

    public Task<CacheRefreshResult> ForceRefreshAsync(bool force, CancellationToken cancellationToken = default) =>
        RefreshAsync(force, cancellationToken);

    private async Task<CacheRefreshResult> RefreshAsync(bool force, CancellationToken cancellationToken)
    {
        var attempt = new RefreshAttempt();
        await _refreshLock.WaitAsync(cancellationToken);
        try
        {
            return await RefreshWhileLockedAsync(force, attempt, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            CleanupDownloadDirectory(attempt.DownloadDirectory, "cancelled");
            throw;
        }
        catch (Exception ex)
        {
            CleanupDownloadDirectory(attempt.DownloadDirectory, "incomplete");
            _logger.LogError(ex,
                "Failed to refresh {CacheName} SQLite cache. Continuing with previously cached file", CacheName);
            _lastRefreshedAt = DateTime.UtcNow;
            return Result("Failed", attempt.Stopwatch, ex.Message);
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private async Task<CacheRefreshResult> RefreshWhileLockedAsync(
        bool force,
        RefreshAttempt attempt,
        CancellationToken cancellationToken)
    {
        if (!_config.Enabled)
            return Result("Failed", attempt.Stopwatch, "SQLite cache is disabled");

        _logger.LogInformation("Asking the data bridge for the latest {CacheName} SQLite file...", CacheName);
        var (artifact, lookupError) = await GetLatestArtifactAsync(cancellationToken);

        if (lookupError is not null)
            return FailedResult(attempt.Stopwatch, lookupError);

        if (artifact is null)
        {
            _logger.LogWarning("No {CacheName} SQLite file available from {Route}", CacheName, LatestArtifactRoute);
            return FailedResult(attempt.Stopwatch, "Data Bridge has no SQLite artifact available");
        }

        var fileName = artifact.FileName;
        if (!fileName.StartsWith(FilePattern, StringComparison.Ordinal))
        {
            _logger.LogWarning(
                "{Route} returned {FileName}, which is not a {CacheName} database (expected prefix {FilePattern})",
                LatestArtifactRoute, fileName, CacheName, FilePattern);
            return FailedResult(attempt.Stopwatch, $"Data Bridge returned an unexpected artifact: {fileName}");
        }

        if (!force && fileName == _cachedFileName)
        {
            _logger.LogInformation("{CacheName} SQLite cache is already up to date: {FileName}", CacheName, fileName);
            _lastRefreshedAt = DateTime.UtcNow;
            return Result("Unchanged", attempt.Stopwatch);
        }

        return await DownloadAndLoadAsync(artifact, attempt, cancellationToken);
    }

    private async Task<(SqliteArtifact? Artifact, string? Error)> GetLatestArtifactAsync(
        CancellationToken cancellationToken)
    {
        using var lookupTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        lookupTimeout.CancelAfter(TimeSpan.FromSeconds(_config.ArtifactLookupTimeoutSeconds));

        try
        {
            return (await _artifactSource.GetLatestAsync(LatestArtifactRoute, lookupTimeout.Token), null);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested && lookupTimeout.IsCancellationRequested)
        {
            _logger.LogWarning("Data Bridge artifact lookup timed out for {CacheName}", CacheName);
            return (null, "Data Bridge artifact lookup timed out");
        }
    }

    private async Task<CacheRefreshResult> DownloadAndLoadAsync(
        SqliteArtifact artifact,
        RefreshAttempt attempt,
        CancellationToken cancellationToken)
    {
        // A unique directory keeps the current file intact while an artifact with the same
        // name is downloaded and validated. Readers already using the old path remain safe.
        attempt.DownloadDirectory = Path.Combine(_config.CachePath, $"{FilePattern}refresh-{Guid.NewGuid():N}");
        Directory.CreateDirectory(attempt.DownloadDirectory);
        var localPath = Path.Combine(attempt.DownloadDirectory, artifact.FileName);
        await _artifactSource.DownloadAsync(artifact, localPath, cancellationToken);

        var rowCount = GetRowCount(localPath);
        var oldPath = GetCurrentDbPath();
        _currentSnapshot = new SqliteSnapshot(localPath, ExtractTimestampFromFileName(artifact.FileName));
        _cachedFileName = artifact.FileName;
        _rowCount = rowCount;
        _lastRefreshedAt = DateTime.UtcNow;
        _isLoaded = true;
        attempt.DownloadDirectory = null;

        _logger.LogInformation(
            "{CacheName} SQLite cache loaded: {FileName}, {RowCount} rows, size: {Size}",
            CacheName, artifact.FileName, rowCount, new FileInfo(localPath).Length);

        if (oldPath is not null)
            await Task.Delay(_config.CleanupDelayMs, cancellationToken);

        CleanupOldCacheFiles(localPath);
        return Result("Reloaded", attempt.Stopwatch);
    }

    private CacheRefreshResult FailedResult(Stopwatch stopwatch, string error)
    {
        _lastRefreshedAt = DateTime.UtcNow;
        return Result("Failed", stopwatch, error);
    }

    private void CleanupDownloadDirectory(string? downloadDirectory, string state)
    {
        if (downloadDirectory is null)
            return;

        try
        {
            Directory.Delete(downloadDirectory, recursive: true);
        }
        catch (Exception cleanupEx)
        {
            _logger.LogWarning(cleanupEx, "Failed to clean up {State} SQLite download", state);
        }
    }

    private CacheRefreshResult Result(string status, Stopwatch stopwatch, string? error = null) =>
        new(CacheName, status, _cachedFileName, _rowCount, DataTimestamp, stopwatch.ElapsedMilliseconds, error);

    private long GetRowCount(string dbPath)
    {
        using var connection = new SqliteConnection($"Data Source={dbPath};Mode=ReadOnly");
        connection.Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = RowCountSql;
        return Convert.ToInt64(cmd.ExecuteScalar(), CultureInfo.InvariantCulture);
    }

    private DateTime? ExtractTimestampFromFileName(string fileName)
    {
        var name = Path.GetFileNameWithoutExtension(fileName);
        var timestampPart = name[FilePattern.Length..];

        if (DateTime.TryParseExact(timestampPart, TimestampFormat,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
            out var result))
        {
            return result;
        }

        return null;
    }

    private void CleanupOldCacheFiles(string currentPath)
    {
        try
        {
            foreach (var file in Directory.GetFiles(_config.CachePath, $"{FilePattern}*.sqlite"))
            {
                if (string.Equals(file, currentPath, StringComparison.OrdinalIgnoreCase)) continue;
                File.Delete(file);
                _logger.LogInformation("Cleaned up old cache file: {File}", file);
            }

            foreach (var directory in Directory.GetDirectories(_config.CachePath, $"{FilePattern}refresh-*"))
            {
                if (string.Equals(directory, Path.GetDirectoryName(currentPath), StringComparison.OrdinalIgnoreCase)) continue;
                Directory.Delete(directory, recursive: true);
                _logger.LogInformation("Cleaned up old cache directory: {Directory}", directory);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to cleanup old cache files");
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            _refreshTimer?.Dispose();
            _refreshLock.Dispose();
        }

        _disposed = true;
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    private sealed class RefreshAttempt
    {
        public Stopwatch Stopwatch { get; } = Stopwatch.StartNew();
        public string? DownloadDirectory { get; set; }
    }
}