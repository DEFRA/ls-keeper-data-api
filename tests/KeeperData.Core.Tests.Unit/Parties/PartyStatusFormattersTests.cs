using FluentAssertions;
using KeeperData.Core.Domain.Enums;
using KeeperData.Core.Domain.Parties.Formatters;
using KeeperData.Core.Extensions;

namespace KeeperData.Core.Tests.Unit.Parties;

public class PartyStatusFormattersTests
{
    [Theory]
    [InlineData(false, PartyStatusType.Active)]
    [InlineData(true, PartyStatusType.Inactive)]
    public void FormatPartyStatus_ReturnsExpectedStatus(bool deleted, PartyStatusType expectedStatus)
    {
        var result = PartyStatusFormatters.FormatPartyStatus(deleted);

        result.Should().Be(expectedStatus.GetDescription());
    }

    [Fact]
    public void FormatPartyStatus_WithDeletedFalse_ReturnsActive_RegardlessOfEndDate()
    {
        // Arrange
        var deleted = false;

        // Act
        var result = PartyStatusFormatters.FormatPartyStatus(deleted);

        // Assert
        result.Should().Be(PartyStatusType.Active.GetDescription(), "status should be based on deleted flag, not end date");
    }

    [Fact]
    public void FormatPartyStatus_WithDeletedTrue_ReturnsInactive_RegardlessOfNoEndDate()
    {
        // Arrange
        var deleted = true;

        // Act
        var result = PartyStatusFormatters.FormatPartyStatus(deleted);

        // Assert
        result.Should().Be(PartyStatusType.Inactive.GetDescription(), "status should be based on deleted flag, not presence of end date");
    }
}