using Amazon.S3;
using Amazon.S3.Model;
using FluentAssertions;
using KeeperData.Infrastructure.Services;
using KeeperData.Infrastructure.Storage.Clients;
using KeeperData.Infrastructure.Storage.Configuration;
using KeeperData.Infrastructure.Storage.Factories;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace KeeperData.Infrastructure.Tests.Unit.Services;

public class CphSqliteCacheServiceTests : IDisposable
{
    private readonly Mock<IS3ClientFactory> _mockFactory;
    private readonly Mock<IAmazonS3> _mockS3;
    private readonly CphSqliteCacheConfiguration _config;
    private readonly CphSqliteCacheService _service;
    private readonly string _tempDir;

    public CphSqliteCacheServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"cph-cache-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);

        _mockFactory = new Mock<IS3ClientFactory>();
        _mockS3 = new Mock<IAmazonS3>();

        _mockFactory.Setup(f => f.GetClient<CphSqliteStorageClient>()).Returns(_mockS3.Object);
        _mockFactory.Setup(f => f.GetClientBucketName<CphSqliteStorageClient>()).Returns("test-bucket");

        _config = new CphSqliteCacheConfiguration
        {
            Enabled = true,
            CachePath = _tempDir,
            S3Prefix = "views/",
            FilePattern = "cphs_",
            RefreshIntervalHours = 24
        };

        _service = new CphSqliteCacheService(
            _mockFactory.Object,
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
    public async Task RefreshCache_WhenNoFilesInS3_RemainsUnloaded()
    {
        _mockS3.Setup(s => s.ListObjectsV2Async(It.IsAny<ListObjectsV2Request>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ListObjectsV2Response { S3Objects = [] });

        await _service.RefreshCacheAsync(CancellationToken.None);

        _service.IsLoaded.Should().BeFalse();
        _service.GetCurrentDbPath().Should().BeNull();
    }

    [Fact]
    public async Task RefreshCache_DownloadsAndLoadsFile()
    {
        var sqlitePath = CreateTestSqliteFile("cphs_20260630T120000Z.sqlite", ["01/001/0001", "02/002/0002"]);

        SetupS3ListAndGet("views/cphs_20260630T120000Z.sqlite", sqlitePath);

        await _service.RefreshCacheAsync(CancellationToken.None);

        _service.IsLoaded.Should().BeTrue();
        _service.CachedFileName.Should().Be("cphs_20260630T120000Z.sqlite");
        _service.DataTimestamp.Should().Be(new DateTime(2026, 6, 30, 12, 0, 0, DateTimeKind.Utc));
        _service.LastRefreshedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        _service.GetCurrentDbPath().Should().NotBeNull();
        _service.GetCurrentDbPath().Should().EndWith("cphs_20260630T120000Z.sqlite");
    }

    [Fact]
    public async Task GetCurrentDbPath_AfterRefresh_ReturnsValidPath()
    {
        var sqlitePath = CreateTestSqliteFile("cphs_20260630T120000Z.sqlite", ["01/001/0001"]);
        SetupS3ListAndGet("views/cphs_20260630T120000Z.sqlite", sqlitePath);

        await _service.RefreshCacheAsync(CancellationToken.None);

        var path = _service.GetCurrentDbPath();
        path.Should().NotBeNull();
        File.Exists(path).Should().BeTrue();
    }

    [Fact]
    public async Task RefreshCache_WhenSameFile_SkipsDownload()
    {
        var sqlitePath = CreateTestSqliteFile("cphs_20260630T120000Z.sqlite", ["01/001/0001"]);
        SetupS3ListAndGet("views/cphs_20260630T120000Z.sqlite", sqlitePath);

        await _service.RefreshCacheAsync(CancellationToken.None);
        _service.IsLoaded.Should().BeTrue();

        await _service.RefreshCacheAsync(CancellationToken.None);

        _mockS3.Verify(s => s.GetObjectAsync(It.IsAny<GetObjectRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RefreshCache_PicksLatestFileByKey()
    {
        var oldPath = CreateTestSqliteFile("cphs_20260101T120000Z.sqlite", ["old"]);
        var newPath = CreateTestSqliteFile("cphs_20260630T120000Z.sqlite", ["new"]);

        _mockS3.Setup(s => s.ListObjectsV2Async(It.IsAny<ListObjectsV2Request>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ListObjectsV2Response
            {
                S3Objects =
                [
                    new S3Object { Key = "views/cphs_20260101T120000Z.sqlite", Size = 100 },
                    new S3Object { Key = "views/cphs_20260630T120000Z.sqlite", Size = 100 },
                    new S3Object { Key = "views/other_file.sqlite", Size = 100 }
                ]
            });

        SetupS3GetForKey("views/cphs_20260630T120000Z.sqlite", newPath);

        await _service.RefreshCacheAsync(CancellationToken.None);

        _service.CachedFileName.Should().Be("cphs_20260630T120000Z.sqlite");
    }

    [Fact]
    public async Task RefreshCache_OnError_ContinuesServingOldData()
    {
        var sqlitePath = CreateTestSqliteFile("cphs_20260101T120000Z.sqlite", ["01/001/0001"]);
        SetupS3ListAndGet("views/cphs_20260101T120000Z.sqlite", sqlitePath);
        await _service.RefreshCacheAsync(CancellationToken.None);
        _service.IsLoaded.Should().BeTrue();

        _mockS3.Setup(s => s.ListObjectsV2Async(It.IsAny<ListObjectsV2Request>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AmazonS3Exception("Network error"));

        await _service.RefreshCacheAsync(CancellationToken.None);

        _service.IsLoaded.Should().BeTrue();
        _service.CachedFileName.Should().Be("cphs_20260101T120000Z.sqlite");
        _service.GetCurrentDbPath().Should().NotBeNull();
    }

    [Fact]
    public async Task RefreshCache_DbPathPointsToDownloadedFile()
    {
        var sqlitePath = CreateTestSqliteFile("cphs_20260630T120000Z.sqlite", ["01/001/0001", "02/002/0002", "03/003/0003"]);
        SetupS3ListAndGet("views/cphs_20260630T120000Z.sqlite", sqlitePath);

        await _service.RefreshCacheAsync(CancellationToken.None);

        var dbPath = _service.GetCurrentDbPath()!;
        using var connection = new SqliteConnection($"Data Source={dbPath};Mode=ReadOnly");
        connection.Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM cphs";
        var count = Convert.ToInt32(cmd.ExecuteScalar());
        count.Should().Be(3);
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

    private void SetupS3ListAndGet(string key, string localPath)
    {
        _mockS3.Setup(s => s.ListObjectsV2Async(It.IsAny<ListObjectsV2Request>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ListObjectsV2Response
            {
                S3Objects = [new S3Object { Key = key, Size = new FileInfo(localPath).Length }]
            });

        SetupS3GetForKey(key, localPath);
    }

    private void SetupS3GetForKey(string key, string localPath)
    {
        _mockS3.Setup(s => s.GetObjectAsync(
                It.Is<GetObjectRequest>(r => r.Key == key),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((GetObjectRequest req, CancellationToken _) =>
            {
                var response = new GetObjectResponse
                {
                    ResponseStream = File.OpenRead(localPath)
                };
                return response;
            });
    }
}
