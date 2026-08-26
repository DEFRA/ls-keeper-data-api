using FluentAssertions;
using KeeperData.Core.Storage.Sqlite;
using KeeperData.Infrastructure.Services;
using KeeperData.Infrastructure.Storage.Configuration;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace KeeperData.Infrastructure.Tests.Unit.Services;

public class CphSqliteCacheServiceTests : IDisposable
{
    private readonly Mock<ISqliteArtifactSource> _mockArtifactSource;
    private readonly CphSqliteCacheConfiguration _config;
    private readonly CphSqliteCacheService _service;
    private readonly string _tempDir;

    public CphSqliteCacheServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"cph-cache-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);

        _mockArtifactSource = new Mock<ISqliteArtifactSource>();

        _config = new CphSqliteCacheConfiguration
        {
            Enabled = true,
            CachePath = _tempDir,
            FilePattern = "cphs_",
            LatestArtifactRoute = "api/etl/sqlite/cphs/latest",
            RefreshIntervalHours = 24,
            CleanupDelayMs = 0
        };

        _service = new CphSqliteCacheService(
            _mockArtifactSource.Object,
            _config,
            Mock.Of<ILogger<CphSqliteCacheService>>());
    }

    public void Dispose()
    {
        _service.Dispose();
        try { Directory.Delete(_tempDir, true); } catch { }
    }

    [Fact]
    public void IsLoaded_IsFalseBeforeRefresh()
    {
        _service.IsLoaded.Should().BeFalse();
        _service.CachedFileName.Should().BeNull();
        _service.DataTimestamp.Should().BeNull();
    }

    [Fact]
    public void GetCurrentDbPath_WhenNotLoaded_ReturnsNull()
    {
        _service.GetCurrentDbPath().Should().BeNull();
    }

    [Fact]
    public async Task RefreshCache_WhenBridgeHasNoArtifact_RemainsUnloaded()
    {
        _mockArtifactSource
            .Setup(s => s.GetLatestAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SqliteArtifact?)null);

        await _service.RefreshCacheAsync(CancellationToken.None);

        _service.IsLoaded.Should().BeFalse();
        _service.GetCurrentDbPath().Should().BeNull();
        _mockArtifactSource.Verify(s => s.DownloadAsync(
            It.IsAny<SqliteArtifact>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RefreshCache_RequestsTheConfiguredBridgeRoute()
    {
        SetupArtifact("views/cphs_20260630T120000Z.sqlite", ["01/001/0001"]);

        await _service.RefreshCacheAsync(CancellationToken.None);

        _mockArtifactSource.Verify(s => s.GetLatestAsync(
            "api/etl/sqlite/cphs/latest", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RefreshCache_DownloadsAndLoadsFile()
    {
        SetupArtifact("views/cphs_20260630T120000Z.sqlite", ["01/001/0001", "02/002/0002"]);

        await _service.RefreshCacheAsync(CancellationToken.None);

        _service.IsLoaded.Should().BeTrue();
        _service.CachedFileName.Should().Be("cphs_20260630T120000Z.sqlite");
        _service.DataTimestamp.Should().Be(new DateTime(2026, 6, 30, 12, 0, 0, DateTimeKind.Utc));
        _service.LastRefreshedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        _service.GetCurrentDbPath().Should().EndWith("cphs_20260630T120000Z.sqlite");
    }

    [Fact]
    public async Task GetCurrentDbPath_AfterRefresh_ReturnsValidPath()
    {
        SetupArtifact("views/cphs_20260630T120000Z.sqlite", ["01/001/0001"]);

        await _service.RefreshCacheAsync(CancellationToken.None);

        var path = _service.GetCurrentDbPath();
        path.Should().NotBeNull();
        File.Exists(path).Should().BeTrue();
    }

    [Fact]
    public async Task RefreshCache_WhenSameFile_SkipsDownload()
    {
        SetupArtifact("views/cphs_20260630T120000Z.sqlite", ["01/001/0001"]);

        await _service.RefreshCacheAsync(CancellationToken.None);
        _service.IsLoaded.Should().BeTrue();

        await _service.RefreshCacheAsync(CancellationToken.None);

        _mockArtifactSource.Verify(s => s.DownloadAsync(
            It.IsAny<SqliteArtifact>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RefreshCache_WhenBridgeLookupFails_ContinuesServingOldData()
    {
        SetupArtifact("views/cphs_20260101T120000Z.sqlite", ["01/001/0001"]);
        await _service.RefreshCacheAsync(CancellationToken.None);
        _service.IsLoaded.Should().BeTrue();

        _mockArtifactSource
            .Setup(s => s.GetLatestAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Bridge unavailable"));

        await _service.RefreshCacheAsync(CancellationToken.None);

        _service.IsLoaded.Should().BeTrue();
        _service.CachedFileName.Should().Be("cphs_20260101T120000Z.sqlite");
        _service.GetCurrentDbPath().Should().NotBeNull();
    }

    [Fact]
    public async Task RefreshCache_WhenDownloadFails_ContinuesServingOldData()
    {
        SetupArtifact("views/cphs_20260101T120000Z.sqlite", ["01/001/0001"]);
        await _service.RefreshCacheAsync(CancellationToken.None);

        SetupArtifactMetadata("views/cphs_20260630T120000Z.sqlite");
        _mockArtifactSource
            .Setup(s => s.DownloadAsync(
                It.IsAny<SqliteArtifact>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Presigned URL expired"));

        await _service.RefreshCacheAsync(CancellationToken.None);

        _service.IsLoaded.Should().BeTrue();
        _service.CachedFileName.Should().Be("cphs_20260101T120000Z.sqlite");
    }

    [Fact]
    public async Task RefreshCache_WhenDownloadedFileHasWrongSchema_ContinuesServingOldData()
    {
        SetupArtifact("views/cphs_20260101T120000Z.sqlite", ["01/001/0001"]);
        await _service.RefreshCacheAsync(CancellationToken.None);

        SetupArtifactMetadata("views/cphs_20260630T120000Z.sqlite");
        _mockArtifactSource
            .Setup(s => s.DownloadAsync(
                It.IsAny<SqliteArtifact>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns((SqliteArtifact _, string localPath, CancellationToken __) =>
            {
                File.WriteAllText(localPath, "not a sqlite database");
                return Task.CompletedTask;
            });

        await _service.RefreshCacheAsync(CancellationToken.None);

        _service.CachedFileName.Should().Be("cphs_20260101T120000Z.sqlite");
        _service.GetCurrentDbPath().Should().EndWith("cphs_20260101T120000Z.sqlite");
    }

    [Fact]
    public async Task RefreshCache_DbPathPointsToDownloadedFile()
    {
        SetupArtifact("views/cphs_20260630T120000Z.sqlite", ["01/001/0001", "02/002/0002", "03/003/0003"]);

        await _service.RefreshCacheAsync(CancellationToken.None);

        var dbPath = _service.GetCurrentDbPath()!;
        using var connection = new SqliteConnection($"Data Source={dbPath};Mode=ReadOnly");
        connection.Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM cphs";
        var count = Convert.ToInt32(cmd.ExecuteScalar());
        count.Should().Be(3);
    }

    [Fact]
    public async Task StartAsync_WhenDisabled_DoesNotCallBridgeAndRemainsUnloaded()
    {
        using var service = new CphSqliteCacheService(
            _mockArtifactSource.Object,
            _config with { Enabled = false },
            Mock.Of<ILogger<CphSqliteCacheService>>());

        await service.StartAsync(CancellationToken.None);

        service.IsLoaded.Should().BeFalse();
        _mockArtifactSource.Verify(s => s.GetLatestAsync(
            It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task StartAsync_WhenEnabled_LoadsCacheAndStartsTimer()
    {
        SetupArtifact("views/cphs_20260630T120000Z.sqlite", ["01/001/0001"]);

        await _service.StartAsync(CancellationToken.None);

        _service.IsLoaded.Should().BeTrue();
        _service.CachedFileName.Should().Be("cphs_20260630T120000Z.sqlite");

        await _service.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task StartAsync_WhenEnabled_CreatesCacheDirectoryIfNotExists()
    {
        var newCacheDir = Path.Combine(_tempDir, "subdir_cache");
        using var service = new CphSqliteCacheService(
            _mockArtifactSource.Object,
            _config with { CachePath = newCacheDir },
            Mock.Of<ILogger<CphSqliteCacheService>>());

        _mockArtifactSource
            .Setup(s => s.GetLatestAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SqliteArtifact?)null);

        Directory.Exists(newCacheDir).Should().BeFalse();

        await service.StartAsync(CancellationToken.None);

        Directory.Exists(newCacheDir).Should().BeTrue();

        await service.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task StopAsync_BeforeStart_DoesNotThrow()
    {
        var act = async () => await _service.StopAsync(CancellationToken.None);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task StopAsync_AfterStart_DoesNotThrow()
    {
        SetupArtifact("views/cphs_20260630T120000Z.sqlite", ["01/001/0001"]);
        await _service.StartAsync(CancellationToken.None);

        var act = async () => await _service.StopAsync(CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public void Dispose_CalledTwice_DoesNotThrow()
    {
        using var service = new CphSqliteCacheService(
            _mockArtifactSource.Object,
            _config,
            Mock.Of<ILogger<CphSqliteCacheService>>());

        service.Dispose();

        var act = () => service.Dispose();
        act.Should().NotThrow();
    }

    [Fact]
    public async Task RefreshCache_WhenArtifactDoesNotMatchPattern_RemainsUnloaded()
    {
        SetupArtifactMetadata("views/krds-db_20260630120000.sqlite");

        await _service.RefreshCacheAsync(CancellationToken.None);

        _service.IsLoaded.Should().BeFalse();
        _mockArtifactSource.Verify(s => s.DownloadAsync(
            It.IsAny<SqliteArtifact>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RefreshCache_SameFile_UpdatesLastRefreshedAt()
    {
        SetupArtifact("views/cphs_20260630T120000Z.sqlite", ["01/001/0001"]);

        await _service.RefreshCacheAsync(CancellationToken.None);
        var firstRefreshedAt = _service.LastRefreshedAt;

        await Task.Delay(50);

        await _service.RefreshCacheAsync(CancellationToken.None);

        _service.LastRefreshedAt.Should().BeAfter(firstRefreshedAt!.Value,
            "LastRefreshedAt should update even when the file download is skipped");
    }

    [Fact]
    public async Task RefreshCache_SwapToNewerFile_UpdatesDataTimestampAndFileName()
    {
        SetupArtifact("views/cphs_20260101T120000Z.sqlite", ["01/001/0001"]);
        await _service.RefreshCacheAsync(CancellationToken.None);

        _service.CachedFileName.Should().Be("cphs_20260101T120000Z.sqlite");
        _service.DataTimestamp.Should().Be(new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc));

        SetupArtifact("views/cphs_20260630T120000Z.sqlite", ["02/002/0002"]);

        await _service.RefreshCacheAsync(CancellationToken.None);

        _service.CachedFileName.Should().Be("cphs_20260630T120000Z.sqlite");
        _service.DataTimestamp.Should().Be(new DateTime(2026, 6, 30, 12, 0, 0, DateTimeKind.Utc));
        _service.IsLoaded.Should().BeTrue();
    }

    [Fact]
    public async Task RefreshCache_WhenFilenameTimestampIsUnparseable_DataTimestampIsNull()
    {
        SetupArtifact("views/cphs_not_a_date.sqlite", ["01/001/0001"]);

        await _service.RefreshCacheAsync(CancellationToken.None);

        _service.IsLoaded.Should().BeTrue("the file should still load even with an unparseable timestamp");
        _service.DataTimestamp.Should().BeNull("an unparseable filename should yield a null DataTimestamp");
    }

    [Fact]
    public async Task RefreshCache_Concurrent_SerializesAccessAndDownloadsOnce()
    {
        var downloadStarted = new TaskCompletionSource();
        var downloadCompletion = new TaskCompletionSource();
        var sourcePath = CreateTestSqliteFile("cphs_20260630T120000Z.sqlite", ["01/001/0001"]);
        var downloadCallCount = 0;

        SetupArtifactMetadata("views/cphs_20260630T120000Z.sqlite");

        _mockArtifactSource
            .Setup(s => s.DownloadAsync(
                It.IsAny<SqliteArtifact>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(async (SqliteArtifact _, string localPath, CancellationToken __) =>
            {
                Interlocked.Increment(ref downloadCallCount);
                downloadStarted.TrySetResult();
                await downloadCompletion.Task;
                File.Copy(sourcePath, localPath, overwrite: true);
            });

        var task1 = Task.Run(() => _service.RefreshCacheAsync(CancellationToken.None));

        await downloadStarted.Task; // task1 holds the semaphore and is awaiting the download

        var task2 = Task.Run(() => _service.RefreshCacheAsync(CancellationToken.None));

        downloadCompletion.SetResult();

        await Task.WhenAll(task1, task2);

        downloadCallCount.Should().Be(1,
            "the semaphore serializes access; task2 sees the already-cached filename and skips download");
        _service.IsLoaded.Should().BeTrue();
    }

    private void SetupArtifact(string objectKey, List<string> cphs)
    {
        var sourcePath = CreateTestSqliteFile(objectKey.Split('/').Last(), cphs);

        SetupArtifactMetadata(objectKey);

        _mockArtifactSource
            .Setup(s => s.DownloadAsync(
                It.Is<SqliteArtifact>(a => a.ObjectKey == objectKey),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns((SqliteArtifact _, string localPath, CancellationToken __) =>
            {
                File.Copy(sourcePath, localPath, overwrite: true);
                return Task.CompletedTask;
            });
    }

    private void SetupArtifactMetadata(string objectKey)
    {
        _mockArtifactSource
            .Setup(s => s.GetLatestAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SqliteArtifact
            {
                ObjectKey = objectKey,
                DownloadUrl = $"https://bridge-bucket.example/{objectKey}?X-Amz-Signature=stub",
                Size = 1024,
                LastModified = DateTimeOffset.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(60)
            });
    }

    private string CreateTestSqliteFile(string fileName, List<string> cphs)
    {
        var path = Path.Combine(_tempDir, $"source_{fileName}");
        using (var connection = new SqliteConnection($"Data Source={path}"))
        {
            connection.Open();

            using var createCmd = connection.CreateCommand();
            createCmd.CommandText = "CREATE TABLE cphs (cph TEXT NOT NULL)";
            createCmd.ExecuteNonQuery();

            foreach (var cph in cphs)
            {
                using var insertCmd = connection.CreateCommand();
                insertCmd.CommandText = "INSERT INTO cphs (cph) VALUES (@cph)";
                insertCmd.Parameters.AddWithValue("@cph", cph);
                insertCmd.ExecuteNonQuery();
            }
        }

        SqliteConnection.ClearAllPools();
        return path;
    }
}
