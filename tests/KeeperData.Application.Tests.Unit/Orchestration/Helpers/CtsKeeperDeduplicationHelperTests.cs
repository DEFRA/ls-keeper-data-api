using FluentAssertions;
using KeeperData.Application.Orchestration.Helpers;
using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;

namespace KeeperData.Application.Tests.Unit.Orchestration.Helpers;

public class CtsKeeperDeduplicationHelperTests
{
    [Fact]
    public void DeduplicateKeepersByLatest_WhenKeepersIsNull_ReturnsEmptyList()
    {
        // Act
        var result = CtsKeeperDeduplicationHelper.DeduplicateKeepersByLatest(null);

        // Assert
        result.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void DeduplicateKeepersByLatest_WhenKeepersIsEmpty_ReturnsEmptyList()
    {
        // Arrange
        var keepers = new List<CtsAgentOrKeeper>();

        // Act
        var result = CtsKeeperDeduplicationHelper.DeduplicateKeepersByLatest(keepers);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void DeduplicateKeepersByLatest_WhenSingleKeeper_ReturnsSameKeeper()
    {
        // Arrange
        var keeper = CreateKeeper("PAR123", updatedAt: DateTime.UtcNow);
        var keepers = new List<CtsAgentOrKeeper> { keeper };

        // Act
        var result = CtsKeeperDeduplicationHelper.DeduplicateKeepersByLatest(keepers);

        // Assert
        result.Should().HaveCount(1);
        result[0].Should().Be(keeper);
    }

    [Fact]
    public void DeduplicateKeepersByLatest_WhenDifferentParIds_ReturnsAllKeepers()
    {
        // Arrange
        var keeper1 = CreateKeeper("PAR123");
        var keeper2 = CreateKeeper("PAR456");
        var keeper3 = CreateKeeper("PAR789");
        var keepers = new List<CtsAgentOrKeeper> { keeper1, keeper2, keeper3 };

        // Act
        var result = CtsKeeperDeduplicationHelper.DeduplicateKeepersByLatest(keepers);

        // Assert
        result.Should().HaveCount(3);
        result.Should().Contain(keeper1);
        result.Should().Contain(keeper2);
        result.Should().Contain(keeper3);
    }

    [Fact]
    public void DeduplicateKeepersByLatest_WhenSameParIdWithDifferentUpdatedAt_ReturnsLatestByUpdatedAt()
    {
        // Arrange
        var olderKeeper = CreateKeeper("PAR123", updatedAt: new DateTime(2024, 1, 1));
        var newerKeeper = CreateKeeper("PAR123", updatedAt: new DateTime(2024, 1, 2));
        var keepers = new List<CtsAgentOrKeeper> { olderKeeper, newerKeeper };

        // Act
        var result = CtsKeeperDeduplicationHelper.DeduplicateKeepersByLatest(keepers);

        // Assert
        result.Should().HaveCount(1);
        result[0].Should().Be(newerKeeper);
    }

    [Fact]
    public void DeduplicateKeepersByLatest_WhenSameParIdWithSameUpdatedAtButDifferentCreatedAt_ReturnsLatestByCreatedAt()
    {
        // Arrange
        var sameUpdatedAt = new DateTime(2024, 1, 1);
        var olderKeeper = CreateKeeper("PAR123", updatedAt: sameUpdatedAt, createdAt: new DateTime(2024, 1, 1));
        var newerKeeper = CreateKeeper("PAR123", updatedAt: sameUpdatedAt, createdAt: new DateTime(2024, 1, 2));
        var keepers = new List<CtsAgentOrKeeper> { olderKeeper, newerKeeper };

        // Act
        var result = CtsKeeperDeduplicationHelper.DeduplicateKeepersByLatest(keepers);

        // Assert
        result.Should().HaveCount(1);
        result[0].Should().Be(newerKeeper);
    }

    [Fact]
    public void DeduplicateKeepersByLatest_WhenSameParIdWithSameDatesButDifferentBatchId_ReturnsLatestByBatchId()
    {
        // Arrange
        var sameDate = new DateTime(2024, 1, 1);
        var olderKeeper = CreateKeeper("PAR123", updatedAt: sameDate, createdAt: sameDate, batchId: 1);
        var newerKeeper = CreateKeeper("PAR123", updatedAt: sameDate, createdAt: sameDate, batchId: 2);
        var keepers = new List<CtsAgentOrKeeper> { olderKeeper, newerKeeper };

        // Act
        var result = CtsKeeperDeduplicationHelper.DeduplicateKeepersByLatest(keepers);

        // Assert
        result.Should().HaveCount(1);
        result[0].Should().Be(newerKeeper);
    }

    [Fact]
    public void DeduplicateKeepersByLatest_WhenNullDates_HandlesGracefully()
    {
        // Arrange
        var keeper1 = CreateKeeper("PAR123", updatedAt: null, createdAt: null, batchId: 1);
        var keeper2 = CreateKeeper("PAR123", updatedAt: null, createdAt: null, batchId: 2);
        var keepers = new List<CtsAgentOrKeeper> { keeper1, keeper2 };

        // Act
        var result = CtsKeeperDeduplicationHelper.DeduplicateKeepersByLatest(keepers);

        // Assert
        result.Should().HaveCount(1);
        result[0].Should().Be(keeper2);
    }

    [Theory]
    [InlineData(5, 2, 3)]
    [InlineData(0, 0, 0)]
    [InlineData(1, 1, 0)]
    [InlineData(10, 5, 5)]
    public void GetDeduplicationStats_ReturnsCorrectStats(int originalCount, int deduplicatedCount, int expectedDuplicatesRemoved)
    {
        // Arrange
        var original = CreateKeeperList(originalCount);
        var deduplicated = CreateKeeperList(deduplicatedCount);

        // Act
        var (actualOriginal, actualDeduplicated, actualRemoved) =
            CtsKeeperDeduplicationHelper.GetDeduplicationStats(original, deduplicated);

        // Assert
        actualOriginal.Should().Be(originalCount);
        actualDeduplicated.Should().Be(deduplicatedCount);
        actualRemoved.Should().Be(expectedDuplicatesRemoved);
    }

    [Fact]
    public void GetDeduplicationStats_WhenNullLists_HandlesGracefully()
    {
        // Act
        var (originalCount, deduplicatedCount, duplicatesRemoved) =
            CtsKeeperDeduplicationHelper.GetDeduplicationStats(null, null);

        // Assert
        originalCount.Should().Be(0);
        deduplicatedCount.Should().Be(0);
        duplicatesRemoved.Should().Be(0);
    }

    private static CtsAgentOrKeeper CreateKeeper(
        string parId,
        DateTime? updatedAt = null,
        DateTime? createdAt = null,
        int? batchId = null)
    {
        return new CtsAgentOrKeeper
        {
            PAR_ID = parId,
            UpdatedAtUtc = updatedAt,
            CreatedAtUtc = createdAt,
            BATCH_ID = batchId,
            LID_FULL_IDENTIFIER = "AH-12/345/6789"
        };
    }

    private static List<CtsAgentOrKeeper> CreateKeeperList(int count)
    {
        if (count == 0) return new List<CtsAgentOrKeeper>();

        return Enumerable.Range(1, count)
            .Select(i => CreateKeeper($"PAR{i}"))
            .ToList();
    }
}