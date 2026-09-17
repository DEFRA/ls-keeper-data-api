using FluentAssertions;
using KeeperData.Core.Extensions;

namespace KeeperData.Core.Tests.Unit.Extensions;

public class StringExtensionsTests
{
    [Theory]
    [InlineData("PCPHLANDUSEDBYTCPH", true)]
    [InlineData("pcphlandusedbytcph", true)]
    [InlineData("PcPhLaNdUsEdByTcPh", true)]
    [InlineData("PCPHLANDUSEDBYTCP", false)]
    [InlineData("TCPH", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    [InlineData("   ", false)]
    public void IsPermanentLandHolding_ShouldReturnExpectedResult(string? input, bool expected)
    {
        var result = input.IsPermanentLandHolding();

        result.Should().Be(expected);
    }
}