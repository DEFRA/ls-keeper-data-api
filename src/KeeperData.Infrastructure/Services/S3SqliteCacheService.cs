using Amazon.S3;
using Amazon.S3.Model;
using KeeperData.Core.Storage;
using KeeperData.Infrastructure.Storage.Configuration;
using KeeperData.Infrastructure.Storage.Factories;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace KeeperData.Infrastructure.Services;

/// <summary>
/// Keeps a local copy of the most recent SQLite database published to the views folder, replacing it
/// in place as newer files appear. A refresh failure leaves the previously cached file serving reads.
/// </summary>
/// <typeparam name="TStorageClient">The storage client holding the views folder.</typeparam>
public abstract class S3SqliteCacheService<TStorageClient> : IHostedService, IDisposable
    where TStorageClient : IStorageClient, new()
{
    private readonly IS3ClientFactory _s3ClientFactory;
    private readonly SqliteCacheConfiguration _config;
    private readonly ILogger _logger;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    private volatile string? _currentDbPath;
    private volatile bool _isLoaded;
    private volatile string? _cachedFileName;
    private DateTime? _lastRefreshedAt;
    private DateTime? _dataTimestamp;
    private Timer? _refreshTimer;
    private bool _disposed;

    public bool IsLoaded => _isLoaded;
    public DateTime? LastRefreshedAt => _lastRefreshedAt;
    public DateTime? DataTimestamp => _dataTimestamp;
    public string? CachedFileName => _cachedFileName;

    protected S3SqliteCacheService(
        IS3ClientFactory s3ClientFactory,
        SqliteCacheConfiguration config,
        ILogger logger)
    {
        _s3ClientFactory = s3ClientFactory;
        _config = config;
        _logger = logger;
    }

    /// <summary>The file name prefix identifying the database this cache serves, e.g. "cphs_".</summary>
    protected abstract string FilePattern { get; }

    /// <summary>The format of the timestamp carried in the file name, e.g. "yyyyMMddHHmmss".</summary>
    protected abstract string TimestampFormat { get; }

    /// <summary>A statement proving the downloaded file holds the expected schema.</summary>
    protected abstract string RowCountSql { get; }

    /// <summary>Names the cache in log messages.</summary>
    protected abstract string CacheName { get; }

    public string? GetCurrentDbPath() => _currentDbPath;

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
        await _refreshLock.WaitAsync(cancellationToken);
        try
        {
            _logger.LogInformation("Checking S3 for latest {CacheName} SQLite file...", CacheName);

            var s3Client = _s3ClientFactory.GetClient<TStorageClient>();
            var bucketName = _s3ClientFactory.GetClientBucketName<TStorageClient>();

            var latestKey = await FindLatestKeyAsync(s3Client, bucketName, cancellationToken);

            if (latestKey is null)
            {
                _logger.LogWarning("No {CacheName} SQLite files found in S3 bucket {Bucket} with prefix {Prefix}",
                    CacheName, bucketName, _config.S3Prefix);
                return;
            }

            var fileName = Path.GetFileName(latestKey);

            if (fileName == _cachedFileName)
            {
                _logger.LogInformation("{CacheName} SQLite cache is already up to date: {FileName}", CacheName, fileName);
                _lastRefreshedAt = DateTime.UtcNow;
                return;
            }

            var localPath = Path.Combine(_config.CachePath, fileName);
            await DownloadFileAsync(s3Client, bucketName, latestKey, localPath, cancellationToken);

            var rowCount = GetRowCount(localPath);
            var timestamp = ExtractTimestampFromFileName(fileName);

            var oldPath = _currentDbPath;
            _currentDbPath = localPath;
            _cachedFileName = fileName;
            _dataTimestamp = timestamp;
            _lastRefreshedAt = DateTime.UtcNow;
            _isLoaded = true;

            _logger.LogInformation(
                "{CacheName} SQLite cache loaded: {FileName}, {RowCount} rows, size: {Size}",
                CacheName, fileName, rowCount, new FileInfo(localPath).Length);

            if (oldPath is not null)
            {
                await Task.Delay(_config.CleanupDelayMs, cancellationToken);
                CleanupOldCacheFiles(localPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to refresh {CacheName} SQLite cache. Continuing with previously cached file", CacheName);
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private async Task<string?> FindLatestKeyAsync(
        IAmazonS3 s3Client, string bucketName, CancellationToken cancellationToken)
    {
        var request = new ListObjectsV2Request
        {
            BucketName = bucketName,
            Prefix = _config.S3Prefix
        };

        var response = await s3Client.ListObjectsV2Async(request, cancellationToken);

        return response.S3Objects
            .Where(o => Path.GetFileName(o.Key).StartsWith(FilePattern, StringComparison.Ordinal)
                        && o.Key.EndsWith(".sqlite", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(o => o.Key, StringComparer.Ordinal)
            .Select(o => o.Key)
            .FirstOrDefault();
    }

    private static async Task DownloadFileAsync(
        IAmazonS3 s3Client, string bucketName, string key,
        string localPath, CancellationToken cancellationToken)
    {
        var request = new GetObjectRequest { BucketName = bucketName, Key = key };
        using var response = await s3Client.GetObjectAsync(request, cancellationToken);

        if (response.ResponseStream is not null)
        {
            await using var fileStream = File.Create(localPath);
            await response.ResponseStream.CopyToAsync(fileStream, cancellationToken);
        }
    }

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
                if (!string.Equals(file, currentPath, StringComparison.OrdinalIgnoreCase))
                {
                    File.Delete(file);
                    _logger.LogInformation("Cleaned up old cache file: {File}", file);
                }
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
}
