using FluentAssertions;
using KeeperData.Core.Storage.Sqlite;
using KeeperData.Infrastructure.Storage.Sources;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace KeeperData.Infrastructure.Tests.Unit.Storage.Sources;

public class FakeSqliteArtifactSourceTests : IDisposable
{
    private readonly Mock<ILogger<FakeSqliteArtifactSource>> _loggerMock = new();
    private readonly FakeSqliteArtifactSource _source;
    private readonly string _tempDir;

    public FakeSqliteArtifactSourceTests()
    {
        _source = new FakeSqliteArtifactSource(_loggerMock.Object);
        _tempDir = Path.Combine(Path.GetTempPath(), $"fake-sqlite-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        try
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, true);
            }
        }
        catch
        {
            // Best effort cleanup in tests
        }
    }

    [Fact]
    public async Task GetLatestAsync_ForCphsRoute_ReturnsValidCphArtifact()
    {
        var artifact = await _source.GetLatestAsync("api/etl/sqlite/cphs/latest", CancellationToken.None);

        artifact.Should().NotBeNull();
        artifact!.FileName.Should().StartWith("cphs_");
        artifact.FileName.Should().EndWith(".sqlite");
        artifact.DownloadUrl.Should().StartWith("fake://sqlite/");
        artifact.Size.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetLatestAsync_ForStagingRoute_ReturnsValidReadModelArtifact()
    {
        var artifact = await _source.GetLatestAsync("api/etl/staging/sqlite/latest", CancellationToken.None);

        artifact.Should().NotBeNull();
        artifact!.FileName.Should().StartWith("krds-db_");
        artifact.FileName.Should().EndWith(".sqlite");
        artifact.DownloadUrl.Should().StartWith("fake://sqlite/");
        artifact.Size.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetLatestAsync_ForUnknownRoute_ReturnsNull()
    {
        var artifact = await _source.GetLatestAsync("api/unknown/route", CancellationToken.None);

        artifact.Should().BeNull();
    }

    [Fact]
    public async Task DownloadAsync_ForCphsArtifact_SeedsCphsTable()
    {
        var artifact = await _source.GetLatestAsync("api/etl/sqlite/cphs/latest", CancellationToken.None);
        var dbPath = Path.Combine(_tempDir, artifact!.FileName);

        await _source.DownloadAsync(artifact, dbPath, CancellationToken.None);

        File.Exists(dbPath).Should().BeTrue();

        await using var connection = new SqliteConnection($"Data Source={dbPath};Mode=ReadOnly");
        await connection.OpenAsync();

        await using var countCmd = connection.CreateCommand();
        countCmd.CommandText = "SELECT COUNT(*) FROM cphs;";
        var count = Convert.ToInt64(await countCmd.ExecuteScalarAsync());
        count.Should().Be(15);

        await using var checkCmd = connection.CreateCommand();
        checkCmd.CommandText = "SELECT COUNT(*) FROM cphs WHERE cph = '13/169/0007';";
        var found = Convert.ToInt64(await checkCmd.ExecuteScalarAsync());
        found.Should().Be(1);
    }

    [Fact]
    public async Task DownloadAsync_ForReadModelArtifact_SeedsAllReadModelTables()
    {
        var artifact = await _source.GetLatestAsync("api/etl/staging/sqlite/latest", CancellationToken.None);
        var dbPath = Path.Combine(_tempDir, artifact!.FileName);

        await _source.DownloadAsync(artifact, dbPath, CancellationToken.None);

        File.Exists(dbPath).Should().BeTrue();

        await using var connection = new SqliteConnection($"Data Source={dbPath};Mode=ReadOnly");
        await connection.OpenAsync();

        // 1. Holding
        await using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "SELECT COUNT(*) FROM Holding;";
            Convert.ToInt64(await cmd.ExecuteScalarAsync()).Should().Be(15);
        }

        // 2. Party
        await using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "SELECT COUNT(*) FROM Party;";
            Convert.ToInt64(await cmd.ExecuteScalarAsync()).Should().Be(15);
        }

        // 3. Herd
        await using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "SELECT COUNT(*) FROM Herd;";
            Convert.ToInt64(await cmd.ExecuteScalarAsync()).Should().BeGreaterThan(0);
        }

        // 4. PartyRole
        await using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "SELECT COUNT(*) FROM PartyRole;";
            Convert.ToInt64(await cmd.ExecuteScalarAsync()).Should().BeGreaterThan(0);
        }

        // 5. HoldingAnimalProfile
        await using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "SELECT COUNT(*) FROM HoldingAnimalProfile;";
            Convert.ToInt64(await cmd.ExecuteScalarAsync()).Should().BeGreaterThan(0);
        }

        // Verify worked example 13/169/0007
        await using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "SELECT FeatureName, Postcode FROM Holding WHERE Cph = '13/169/0007';";
            await using var reader = await cmd.ExecuteReaderAsync();
            (await reader.ReadAsync()).Should().BeTrue();
            reader.GetString(0).Should().Be("Land At Test Farm 06");
            reader.GetString(1).Should().Be("CO5 7RR");
        }
    }

    [Fact]
    public async Task GivenSeededReadModel_WhenHoldingDetailRepositoryQueries_ThenReturnsPagedHoldingsAndWorkedExample()
    {
        var artifact = await _source.GetLatestAsync("api/etl/staging/sqlite/latest", CancellationToken.None);
        var dbPath = Path.Combine(_tempDir, artifact!.FileName);
        await _source.DownloadAsync(artifact, dbPath, CancellationToken.None);

        var mockCache = new Mock<KeeperData.Core.Services.IReadModelSqliteCacheService>();
        mockCache.Setup(c => c.GetCurrentDbPath()).Returns(dbPath);

        var repo = new KeeperData.Infrastructure.Database.Repositories.HoldingDetailRepository(mockCache.Object);

        // 1. GetPagedHoldingsAsync
        var (items, totalCount) = await repo.GetPagedHoldingsAsync(1, 10, "asc", "cph");
        totalCount.Should().Be(15);
        items.Should().HaveCount(10);
        items[0].Identifier.Should().Be("10/024/0247");
        items[1].Identifier.Should().Be("13/169/0007");
        items[1].Name.Should().Be("Land At Test Farm 06");
        items[1].Location.Address.PostTown.Should().Be("COLCHESTER");

        // 2. GetHoldingDetailByCphAsync for worked example
        var holding = await repo.GetHoldingDetailByCphAsync("13/169/0007");
        holding.Should().NotBeNull();
        holding!.Identifier.Should().Be("13/169/0007");
        holding.Name.Should().Be("Land At Test Farm 06");
        holding.Location.Address.AddressLine1.Should().Be("Test Farm 06");
        holding.Location.Address.AddressLine2.Should().Be("Layer Road");
        holding.Associations.Should().ContainSingle(a => a.Name == "Green Fields Farming Ltd");
        holding.AllowedSpecies.Should().BeEquivalentTo(["CTT", "SHP"]);
        holding.Marks.Should().HaveCount(2);
    }

    [Fact]
    public async Task GivenSeededCphsDatabase_WhenCphRepositoryQueries_ThenReturnsPagedCphs()
    {
        var artifact = await _source.GetLatestAsync("api/etl/sqlite/cphs/latest", CancellationToken.None);
        var dbPath = Path.Combine(_tempDir, artifact!.FileName);
        await _source.DownloadAsync(artifact, dbPath, CancellationToken.None);

        var mockCache = new Mock<KeeperData.Core.Services.ICphSqliteCacheService>();
        mockCache.Setup(c => c.GetCurrentDbPath()).Returns(dbPath);

        var repo = new KeeperData.Infrastructure.Database.Repositories.CphRepository(mockCache.Object);

        var (items, totalCount) = await repo.GetPagedAsync(1, 10, "asc");
        totalCount.Should().Be(15);
        items.Should().HaveCount(10);
        items[0].Cph.Should().Be("10/024/0247");
        items[1].Cph.Should().Be("13/169/0007");
    }
}