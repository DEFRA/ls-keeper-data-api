using FluentAssertions;
using KeeperData.Core.Domain.Sites.Formatters;

namespace KeeperData.Core.Tests.Unit.Domain.Sites;

public class PremiseActivityTypeFormattersTests
{
    [Theory]
    [InlineData("LAB-OLAB", "OLAB")]
    [InlineData("SLG-RM", "RM")]
    [InlineData("OLAB", "OLAB")]
    [InlineData("RM", "RM")]
    [InlineData("-RM", "RM")]
    [InlineData("  SLG-RM  ", "RM")]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("   ", "")]
    public void TrimFacilityActivityCode_ReturnsExpected(string? input, string expected)
    {
        var result = PremiseActivityTypeFormatters.TrimFacilityActivityCode(input);

        result.Should().Be(expected);
    }
}