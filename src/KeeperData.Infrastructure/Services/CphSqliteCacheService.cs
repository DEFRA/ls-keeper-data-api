using Amazon.S3;
using Amazon.S3.Model;
using KeeperData.Core.Services;
using KeeperData.Infrastructure.Storage.Clients;
using KeeperData.Infrastructure.Storage.Configuration;
using KeeperData.Infrastructure.Storage.Factories;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KeeperData.Infrastructure.Services;

public class CphSqliteCacheService : ICphSqliteCacheService, IHostedService, IDisposable
{
    private readonly IS3ClientFactory _s3ClientFactory;
    private readonly CphSqliteCacheConfiguration _config;
    private readonly ILogger<CphSqliteCacheService> _logger;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    private volatile string? _currentDbPath;
    private volatile bool _isLoaded;
    private volatile string? _cachedFileName;
    private DateTime? _lastRefreshedAt;
    private DateTime? _dataTimestamp;
    private Timer? _refreshTimer;

    public bool IsLoaded => _isLoaded;
    public DateTime? LastRefreshedAt => _lastRefreshedAt;
    public DateTime? DataTimestamp => _dataTimestamp;
    public string? CachedFileName => _cachedFileName;

    public CphSqliteCacheService(
        IS3ClientFactory s3ClientFactory,
        CphSqliteCacheConfiguration config,
        ILogger<CphSqliteCacheService> logger)
    {
        _s3ClientFactory = s3ClientFactory;
        _config = config;
        _logger = logger;
    }

    public string? GetCurrentDbPath() => _currentDbPath;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_config.Enabled)
        {
            _logger.LogInformation("CPH SQLite cache is disabled");
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
            "CPH SQLite cache refresh timer started with interval {IntervalHours}h",
            _config.RefreshIntervalHours);
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
            _logger.LogInformation("Checking S3 for latest CPH SQLite file...");

            var s3Client = _s3ClientFactory.GetClient<CphSqliteStorageClient>();
            var bucketName = _s3ClientFactory.GetClientBucketName<CphSqliteStorageClient>();

            var latestKey = await FindLatestCphSqliteKeyAsync(s3Client, bucketName, cancellationToken);

            if (latestKey is null)
            {
                _logger.LogWarning("No CPH SQLite files found in S3 bucket {Bucket} with prefix {Prefix}",
                    bucketName, _config.S3Prefix);
                return;
            }

            var fileName = Path.GetFileName(latestKey);

            if (fileName == _cachedFileName)
            {
                _logger.LogInformation("CPH SQLite cache is already up to date: {FileName}", fileName);
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
                "CPH SQLite cache loaded: {FileName}, {RowCount} rows, size: {Size}",
                fileName, rowCount, new FileInfo(localPath).Length);

            if (oldPath is not null)
            {
                await Task.Delay(5000, cancellationToken);
                CleanupOldCacheFiles(localPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh CPH SQLite cache. Continuing with previously cached file");
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private async Task<string?> FindLatestCphSqliteKeyAsync(
        IAmazonS3 s3Client, string bucketName, CancellationToken cancellationToken)
    {
        var request = new ListObjectsV2Request
        {
            BucketName = bucketName,
            Prefix = _config.S3Prefix
        };

        var response = await s3Client.ListObjectsV2Async(request, cancellationToken);

        return response.S3Objects
            .Where(o => Path.GetFileName(o.Key).StartsWith(_config.FilePattern, StringComparison.Ordinal)
                        && o.Key.EndsWith(".sqlite", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(o => o.Key)
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

    private static int GetRowCount(string dbPath)
    {
        using var connection = new SqliteConnection($"Data Source={dbPath};Mode=ReadOnly");
        connection.Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM cphs";
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    private static DateTime? ExtractTimestampFromFileName(string fileName)
    {
        var name = Path.GetFileNameWithoutExtension(fileName);
        var timestampPart = name.Replace("cphs_", "");

        if (DateTime.TryParseExact(timestampPart, "yyyyMMdd'T'HHmmss'Z'",
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal,
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
            foreach (var file in Directory.GetFiles(_config.CachePath, "cphs_*.sqlite"))
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

    public void Dispose()
    {
        _refreshTimer?.Dispose();
        _refreshLock.Dispose();
        GC.SuppressFinalize(this);
    }
}
