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

        GC.SuppressFinalize(this);
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
            Convert.ToInt64(await cmd.ExecuteScalarAsync()).Should().Be(344);
        }

        // 2. Party
        await using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "SELECT COUNT(*) FROM Party;";
            Convert.ToInt64(await cmd.ExecuteScalarAsync()).Should().Be(345);
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

        mockCache.Setup(c => c.GetCurrentSnapshot()).Returns(new SqliteSnapshot(dbPath, null));

        var repo = new KeeperData.Infrastructure.Database.Repositories.HoldingDetailRepository(mockCache.Object);

        // 1. GetPagedHoldingsAsync
        var (items, totalCount, _) = await repo.GetPagedHoldingsAsync(1, 10, "asc", "cph");
        totalCount.Should().Be(344);
        items.Should().HaveCount(10);
        items[0].Identifier.Should().Be("10/024/0247");
        items[2].Identifier.Should().Be("13/169/0007");
        items[2].Name.Should().Be("Land At Test Farm 06");
        items[2].Location.Address.PostTown.Should().Be("COLCHESTER");

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

    [Theory]
    [InlineData("green fields", "13/169/0007")]
    [InlineData("CO5 7RR", "13/169/0007")]
    [InlineData("131690007", "13/169/0007")]
    public async Task GivenSeededReadModel_WhenSearchIndexIsBuilt_ThenSearchesTheArtifact(string search, string expectedCph)
    {
        var artifact = await _source.GetLatestAsync("api/etl/staging/sqlite/latest", CancellationToken.None);
        var dbPath = Path.Combine(_tempDir, artifact!.FileName);
        await _source.DownloadAsync(artifact, dbPath, CancellationToken.None);
        var (indexPath, documentCount) = await KeeperData.Infrastructure.Services.HoldingSearchIndex.BuildAsync(dbPath, CancellationToken.None);
        documentCount.Should().Be(344);

        var mockCache = new Mock<KeeperData.Core.Services.IReadModelSqliteCacheService>();
        mockCache.Setup(c => c.GetCurrentSnapshot()).Returns(new SqliteSnapshot(dbPath, null, indexPath));
        var repo = new KeeperData.Infrastructure.Database.Repositories.HoldingDetailRepository(mockCache.Object);

        var result = await repo.SearchHoldingsAsync(1, 10, "asc", "cph", search);

        result.Items.Should().Contain(x => x.Identifier == expectedCph);
    }
}