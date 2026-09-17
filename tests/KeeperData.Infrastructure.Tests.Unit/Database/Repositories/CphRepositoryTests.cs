using FluentAssertions;
using KeeperData.Core.Services;
using KeeperData.Infrastructure.Database.Repositories;
using Microsoft.Data.Sqlite;
using Moq;
using Xunit;

namespace KeeperData.Infrastructure.Tests.Unit.Database.Repositories;

public class CphRepositoryTests : IDisposable
{
    private readonly Mock<ICphSqliteCacheService> _mockCacheService;
    private readonly CphRepository _repository;
    private readonly string _tempDir;

    public CphRepositoryTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"cph-repo-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);

        _mockCacheService = new Mock<ICphSqliteCacheService>();
        _repository = new CphRepository(_mockCacheService.Object);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, true); } catch { }
    }

    [Fact]
    public async Task GetPagedAsync_WhenNoDbPath_ReturnsEmpty()
    {
        _mockCacheService.Setup(c => c.GetCurrentDbPath()).Returns((string?)null);

        var (items, total) = await _repository.GetPagedAsync(1, 10, null);

        items.Should().BeEmpty();
        total.Should().Be(0);
    }

    [Fact]
    public async Task GetPagedAsync_ReturnsPaginatedData()
    {
        var dbPath = CreateTestSqliteFile(Enumerable.Range(1, 30).Select(i => $"01/001/{i:D4}").ToList());
        _mockCacheService.Setup(c => c.GetCurrentDbPath()).Returns(dbPath);

        var (items, total) = await _repository.GetPagedAsync(1, 10, "asc");

        items.Should().HaveCount(10);
        total.Should().Be(30);
        items[0].Cph.Should().Be("01/001/0001");
    }

    [Fact]
    public async Task GetPagedAsync_Page2_ReturnsCorrectOffset()
    {
        var dbPath = CreateTestSqliteFile(Enumerable.Range(1, 30).Select(i => $"01/001/{i:D4}").ToList());
        _mockCacheService.Setup(c => c.GetCurrentDbPath()).Returns(dbPath);

        var (items, _) = await _repository.GetPagedAsync(3, 10, "asc");

        items.Should().HaveCount(10);
        items[0].Cph.Should().Be("01/001/0021");
    }

    [Fact]
    public async Task GetPagedAsync_SortDescending_ReturnsReversedOrder()
    {
        var cphs = new List<string> { "01/001/0001", "99/999/9999", "50/500/5000" };
        var dbPath = CreateTestSqliteFile(cphs);
        _mockCacheService.Setup(c => c.GetCurrentDbPath()).Returns(dbPath);

        var (items, _) = await _repository.GetPagedAsync(1, 10, "desc");

        items[0].Cph.Should().Be("99/999/9999");
        items[2].Cph.Should().Be("01/001/0001");
    }

    [Fact]
    public async Task GetPagedAsync_DefaultSort_ReturnsAscending()
    {
        var cphs = new List<string> { "99/999/9999", "01/001/0001", "50/500/5000" };
        var dbPath = CreateTestSqliteFile(cphs);
        _mockCacheService.Setup(c => c.GetCurrentDbPath()).Returns(dbPath);

        var (items, _) = await _repository.GetPagedAsync(1, 10, null);

        items[0].Cph.Should().Be("01/001/0001");
        items[1].Cph.Should().Be("50/500/5000");
        items[2].Cph.Should().Be("99/999/9999");
    }

    [Fact]
    public async Task GetPagedAsync_TotalCount_IsCorrectRegardlessOfPage()
    {
        var dbPath = CreateTestSqliteFile(Enumerable.Range(1, 50).Select(i => $"01/001/{i:D4}").ToList());
        _mockCacheService.Setup(c => c.GetCurrentDbPath()).Returns(dbPath);

        var (_, total1) = await _repository.GetPagedAsync(1, 10, null);
        var (_, total5) = await _repository.GetPagedAsync(5, 10, null);

        total1.Should().Be(50);
        total5.Should().Be(50);
    }

    [Fact]
    public async Task GetPagedAsync_LastPage_ReturnsRemainingItems()
    {
        var dbPath = CreateTestSqliteFile(Enumerable.Range(1, 25).Select(i => $"01/001/{i:D4}").ToList());
        _mockCacheService.Setup(c => c.GetCurrentDbPath()).Returns(dbPath);

        var (items, total) = await _repository.GetPagedAsync(3, 10, "asc");

        items.Should().HaveCount(5);
        total.Should().Be(25);
    }

    [Fact]
    public async Task GetPagedAsync_UsesEfCoreLinq()
    {
        var dbPath = CreateTestSqliteFile(["01/001/0001", "02/002/0002"]);
        _mockCacheService.Setup(c => c.GetCurrentDbPath()).Returns(dbPath);

        var (items, total) = await _repository.GetPagedAsync(1, 100, "asc");

        items.Should().HaveCount(2);
        total.Should().Be(2);
        items.Select(i => i.Cph).Should().BeInAscendingOrder();
    }

    private string CreateTestSqliteFile(List<string> cphs)
    {
        var path = Path.Combine(_tempDir, $"cphs_{Guid.NewGuid():N}.sqlite");
        using var connection = new SqliteConnection($"Data Source={path}");
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

        return path;
    }
}