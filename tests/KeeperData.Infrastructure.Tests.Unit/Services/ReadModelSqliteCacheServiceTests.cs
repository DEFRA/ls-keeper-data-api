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
    private readonly Mock<ILogger<ReadModelSqliteCacheService>> _mockLogger = new();
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

        _mockLogger.Setup(l => l.IsEnabled(LogLevel.Information)).Returns(true);

        _service = new ReadModelSqliteCacheService(
            _mockArtifactSource.Object,
            _config,
            _mockLogger.Object);
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
    public async Task RefreshCache_PublishesPathAndTimestampTogetherWithoutChangingCapturedSnapshot()
    {
        _service.GetCurrentSnapshot().Should().BeNull();
        SetupArtifact("views/krds-db_20260821070003.sqlite", withPartyTable: true);
        await _service.RefreshCacheAsync(CancellationToken.None);
        var original = _service.GetCurrentSnapshot();

        SetupArtifact("views/krds-db_20260822080004.sqlite", withPartyTable: true);
        await _service.RefreshCacheAsync(CancellationToken.None);
        var current = _service.GetCurrentSnapshot();

        original.Should().NotBeNull();
        original!.DbPath.Should().EndWith("krds-db_20260821070003.sqlite");
        original.DataTimestamp.Should().Be(new DateTime(2026, 8, 21, 7, 0, 3, DateTimeKind.Utc));
        current.Should().NotBeNull().And.NotBeSameAs(original);
        current!.DbPath.Should().Be(_service.GetCurrentDbPath()).And.EndWith("krds-db_20260822080004.sqlite");
        current.DataTimestamp.Should().Be(_service.DataTimestamp)
            .And.Be(new DateTime(2026, 8, 22, 8, 0, 4, DateTimeKind.Utc));
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
    public async Task RefreshCache_WhenReplacementIndexCannotBuild_KeepsExistingSnapshot()
    {
        SetupArtifact("views/krds-db_20260821070003.sqlite", withPartyTable: true);
        await _service.RefreshCacheAsync(CancellationToken.None);
        var original = _service.GetCurrentSnapshot();

        SetupArtifact("views/krds-db_20260822080004.sqlite", withPartyTable: true, withHoldingTable: false);
        var result = await _service.ForceRefreshAsync(false);

        result.Status.Should().Be("Failed");
        _service.GetCurrentSnapshot().Should().BeSameAs(original);
        File.Exists(original!.SearchIndexPath).Should().BeTrue();
    }

    [Fact]
    public async Task RefreshCache_WhenInitialIndexCannotBuild_LoadsReadModelAndRetriesIndex()
    {
        const string artifact = "views/krds-db_20260821070003.sqlite";
        SetupArtifact(artifact, withPartyTable: true, blockIndexCreation: true);

        var firstRefresh = await _service.ForceRefreshAsync(false);

        firstRefresh.Status.Should().Be("Reloaded");
        _service.IsLoaded.Should().BeTrue();
        var firstSnapshot = _service.GetCurrentSnapshot();
        firstSnapshot!.SearchIndexPath.Should().BeNull();

        SetupArtifact(artifact, withPartyTable: true);
        var retry = await _service.ForceRefreshAsync(false);

        retry.Status.Should().Be("Reloaded");
        _service.GetCurrentSnapshot()!.SearchIndexPath.Should().NotBeNull();
        File.Exists(_service.GetCurrentSnapshot()!.SearchIndexPath).Should().BeTrue();
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

    [Fact]
    public async Task RefreshCache_WhenCancelledDuringIndexBuild_ThrowsOperationCanceledException()
    {
        const string artifact = "views/krds-db_20260821070003.sqlite";
        using var cts = new CancellationTokenSource();
        var sourcePath = CreateReadModelFile("krds-db_20260821070003.sqlite", withPartyTable: true, withHoldingTable: true);

        _mockArtifactSource
            .Setup(s => s.GetLatestAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SqliteArtifact
            {
                ObjectKey = artifact,
                DownloadUrl = $"https://bridge-bucket.example/{artifact}?X-Amz-Signature=stub",
                Size = 2048,
                LastModified = DateTimeOffset.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(60)
            });

        _mockArtifactSource
            .Setup(s => s.DownloadAsync(It.IsAny<SqliteArtifact>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns((SqliteArtifact _, string localPath, CancellationToken __) =>
            {
                File.Copy(sourcePath, localPath, overwrite: true);
                cts.Cancel();
                return Task.CompletedTask;
            });

        var act = async () => await _service.RefreshCacheAsync(cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    private void SetupArtifact(string objectKey, bool withPartyTable, bool withHoldingTable = true, bool blockIndexCreation = false)
    {
        var sourcePath = CreateReadModelFile(objectKey.Split('/').Last(), withPartyTable, withHoldingTable);

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
                if (blockIndexCreation)
                    Directory.CreateDirectory(Path.Combine(Path.GetDirectoryName(localPath)!, "holdings-search.sqlite"));
                return Task.CompletedTask;
            });
    }

    private string CreateReadModelFile(string fileName, bool withPartyTable, bool withHoldingTable)
    {
        var path = Path.Combine(_tempDir, $"source_{fileName}");
        if (File.Exists(path))
            File.Delete(path);

        using (var connection = new SqliteConnection($"Data Source={path}"))
        {
            connection.Open();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = withPartyTable && withHoldingTable
                ? """
                  CREATE TABLE Party (Id TEXT NOT NULL, SourcePartyId TEXT, OrganisationName TEXT, PersonTitle TEXT, GivenName TEXT, Initials TEXT, FamilyName TEXT, Email TEXT, Mobile TEXT, Telephone TEXT);
                  CREATE TABLE PartyRole (Id TEXT NOT NULL, PartyId TEXT, HoldingId TEXT);
                  CREATE TABLE Holding (Id TEXT NOT NULL, Cph TEXT, FeatureName TEXT, CphType TEXT, Udprn TEXT,
                      PaonDescription TEXT, PaonStartNumber TEXT, PaonStartNumberSuffix TEXT,
                      PaonEndNumber TEXT, PaonEndNumberSuffix TEXT, Street TEXT, Locality TEXT,
                      Town TEXT, Postcode TEXT, OsMapReference TEXT);
                  """
                : withPartyTable
                    ? "CREATE TABLE Party (Id TEXT NOT NULL)"
                    : "CREATE TABLE Something (Id TEXT NOT NULL)";
            cmd.ExecuteNonQuery();
        }

        SqliteConnection.ClearAllPools();
        return path;
    }
}