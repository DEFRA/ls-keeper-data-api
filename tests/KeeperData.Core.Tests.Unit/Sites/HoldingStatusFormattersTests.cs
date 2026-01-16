using FluentAssertions;
using KeeperData.Core.Domain.Enums;
using KeeperData.Core.Domain.Sites.Formatters;
using KeeperData.Core.Extensions;

namespace KeeperData.Core.Tests.Unit.Sites;

public class HoldingStatusFormattersTests
{
    [Theory]
    [InlineData(false, HoldingStatusType.Active)]
    [InlineData(true, HoldingStatusType.Inactive)]
    public void FormatHoldingStatus_ReturnsExpectedStatus(bool deleted, HoldingStatusType expectedStatus)
    {
        var result = HoldingStatusFormatters.FormatHoldingStatus(deleted);

        result.Should().Be(expectedStatus.GetDescription());
    }

    [Fact]
    public void FormatHoldingStatus_WithDeletedFalse_ReturnsActive_RegardlessOfEndDate()
    {
        // Arrange
        var deleted = false;

        // Act
        var result = HoldingStatusFormatters.FormatHoldingStatus(deleted);

        // Assert
        result.Should().Be(HoldingStatusType.Active.GetDescription(), "status should be based on deleted flag, not end date");
    }

    [Fact]
    public void FormatHoldingStatus_WithDeletedTrue_ReturnsInactive_RegardlessOfNoEndDate()
    {
        // Arrange
        var deleted = true;

        // Act
        var result = HoldingStatusFormatters.FormatHoldingStatus(deleted);

        // Assert
        result.Should().Be(HoldingStatusType.Inactive.GetDescription(), "status should be based on deleted flag, not presence of end date");
    }
}