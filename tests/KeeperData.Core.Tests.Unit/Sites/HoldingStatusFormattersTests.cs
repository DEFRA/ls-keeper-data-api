using FluentAssertions;
using KeeperData.Core.Domain.Sites.Formatters;

namespace KeeperData.Core.Tests.Unit.Sites;

public class HoldingStatusFormattersTests
{
    [Theory]
    [InlineData(false, "Active")]
    [InlineData(true, "Inactive")]
    public void FormatHoldingStatus_ReturnsExpectedStatus(bool deleted, string expectedStatus)
    {
        var result = HoldingStatusFormatters.FormatHoldingStatus(deleted);

        result.Should().Be(expectedStatus);
    }

    [Fact]
    public void FormatHoldingStatus_WithDeletedFalse_ReturnsActive_RegardlessOfEndDate()
    {
        // Arrange
        var deleted = false;
        var endDate = DateTime.Now.AddDays(-1);

        // Act
        var result = HoldingStatusFormatters.FormatHoldingStatus(deleted);

        // Assert
        result.Should().Be("Active", "status should be based on deleted flag, not end date");
    }

    [Fact]
    public void FormatHoldingStatus_WithDeletedTrue_ReturnsInactive_RegardlessOfNoEndDate()
    {
        // Arrange
        var deleted = true;

        // Act
        var result = HoldingStatusFormatters.FormatHoldingStatus(deleted);

        // Assert
        result.Should().Be("Inactive", "status should be based on deleted flag, not presence of end date");
    }
}