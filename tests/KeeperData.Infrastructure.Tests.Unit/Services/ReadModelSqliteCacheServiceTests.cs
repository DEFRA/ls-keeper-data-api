using FluentAssertions;
using KeeperData.Core.Storage.Sqlite;
using KeeperData.Infrastructure.Services;
using KeeperData.Infrastructure.Storage.Configuration;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace KeeperData.Infrastructure.Tests.Unit.Services;

public class ReadModelSqliteCacheServiceTests : IDisposable
{
    private readonly Mock<ISqliteArtifactSource> _mockArtifactSource = new();
    private readonly ReadModelSqliteCacheConfiguration _config;
    private readonly ReadModelSqliteCacheService _service;
    private readonly string _tempDir;

    public ReadModelSqliteCacheServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"read-model-cache-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);

        _config = new ReadModelSqliteCacheConfiguration
        {
            Enabled = true,
            CachePath = _tempDir,
            FilePattern = "krds-db_",
            LatestArtifactRoute = "api/etl/staging/sqlite/latest",
            RefreshIntervalHours = 24,
            CleanupDelayMs = 0
        };

        _service = new ReadModelSqliteCacheService(
            _mockArtifactSource.Object,
            _config,
            Mock.Of<ILogger<ReadModelSqliteCacheService>>());
    }

    public void Dispose()
    {
        _service.Dispose();
        try { Directory.Delete(_tempDir, true); } catch { }
    }

    [Fact]
    public async Task RefreshCache_LoadsTheReadModelFromTheStagingRoute()
    {
        SetupArtifact("views/krds-db_20260821070003.sqlite", withPartyTable: true);

        await _service.RefreshCacheAsync(CancellationToken.None);

        _service.IsLoaded.Should().BeTrue();
        _service.CachedFileName.Should().Be("krds-db_20260821070003.sqlite");
        _service.DataTimestamp.Should().Be(new DateTime(2026, 8, 21, 7, 0, 3, DateTimeKind.Utc));

        _mockArtifactSource.Verify(s => s.GetLatestAsync(
            "api/etl/staging/sqlite/latest", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RefreshCache_WhenTheReadModelLacksTheExpectedSchema_RemainsUnloaded()
    {
        SetupArtifact("views/krds-db_20260821070003.sqlite", withPartyTable: false);

        await _service.RefreshCacheAsync(CancellationToken.None);

        _service.IsLoaded.Should().BeFalse("a database without a Party table cannot answer association queries");
        _service.GetCurrentDbPath().Should().BeNull();
    }

    [Fact]
    public async Task RefreshCache_WhenTheBridgeReturnsTheLegacyCphDatabase_RemainsUnloaded()
    {
        _mockArtifactSource
            .Setup(s => s.GetLatestAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SqliteArtifact
            {
                ObjectKey = "views/cphs_20260630T120000Z.sqlite",
                DownloadUrl = "https://bridge-bucket.example/file?X-Amz-Signature=stub"
            });

        await _service.RefreshCacheAsync(CancellationToken.None);

        _service.IsLoaded.Should().BeFalse();
        _mockArtifactSource.Verify(s => s.DownloadAsync(
            It.IsAny<SqliteArtifact>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private void SetupArtifact(string objectKey, bool withPartyTable)
    {
        var sourcePath = CreateReadModelFile(objectKey.Split('/').Last(), withPartyTable);

        _mockArtifactSource
            .Setup(s => s.GetLatestAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SqliteArtifact
            {
                ObjectKey = objectKey,
                DownloadUrl = $"https://bridge-bucket.example/{objectKey}?X-Amz-Signature=stub",
                Size = 2048,
                LastModified = DateTimeOffset.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(60)
            });

        _mockArtifactSource
            .Setup(s => s.DownloadAsync(
                It.IsAny<SqliteArtifact>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns((SqliteArtifact _, string localPath, CancellationToken __) =>
            {
                File.Copy(sourcePath, localPath, overwrite: true);
                return Task.CompletedTask;
            });
    }

    private string CreateReadModelFile(string fileName, bool withPartyTable)
    {
        var path = Path.Combine(_tempDir, $"source_{fileName}");

        using (var connection = new SqliteConnection($"Data Source={path}"))
        {
            connection.Open();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = withPartyTable
                ? "CREATE TABLE Party (Id TEXT NOT NULL, Email TEXT)"
                : "CREATE TABLE Something (Id TEXT NOT NULL)";
            cmd.ExecuteNonQuery();
        }

        SqliteConnection.ClearAllPools();
        return path;
    }
}