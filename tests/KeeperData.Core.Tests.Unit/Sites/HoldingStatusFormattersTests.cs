using FluentAssertions;
using KeeperData.Core.Domain.Sites.Formatters;

namespace KeeperData.Core.Tests.Unit.Sites;

public class HoldingStatusFormattersTests
{
    [Theory]
    [InlineData(null, "Active")]
    [InlineData("0001-01-01", "Active")]
    [InlineData("2023-01-01", "Inactive")]
    [InlineData("2050-12-31", "Inactive")]
    public void FormatHoldingStatus_ReturnsExpectedStatus(string? dateString, string expectedStatus)
    {
        DateTime? endDate = dateString is null ? null : DateTime.Parse(dateString);

        var result = HoldingStatusFormatters.FormatHoldingStatus(endDate);

        result.Should().Be(expectedStatus);
    }
}