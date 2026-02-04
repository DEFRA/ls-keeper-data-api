using FluentAssertions;
using KeeperData.Core.Domain.Sites.Formatters;

namespace KeeperData.Core.Tests.Unit.Domain.Sites;

public class ProductionUsageCodeFormattersTests
{
    [Theory]
    [InlineData("CTT-BEEF", "BEEF")]
    [InlineData("SHP-LAMB", "LAMB")]
    [InlineData("PIG", "PIG")]
    [InlineData("CTT", "CTT")]
    [InlineData("-BEEF", "BEEF")]
    [InlineData("  CTT-BEEF  ", "BEEF")]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("   ", "")]
    public void TrimProductionUsageCodeHolding_ReturnsExpected(string? input, string expected)
    {
        var result = ProductionUsageCodeFormatters.TrimProductionUsageCodeHolding(input);

        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("CTT-BEEF", "BEEF")]
    [InlineData("SHP-LAMB", "LAMB")]
    [InlineData("PIG", "PIG")]
    [InlineData("CTT", "CTT")]
    [InlineData("-BEEF", "BEEF")]
    [InlineData("CTT-BEEF-ADLR", "BEEF")]
    [InlineData("SHP-LAMB-DAIRY", "LAMB")]
    [InlineData("  CTT-BEEF-ADLR  ", "BEEF")]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("   ", "")]
    public void TrimProductionUsageCodeHerd_ReturnsExpected(string? input, string expected)
    {
        var result = ProductionUsageCodeFormatters.TrimProductionUsageCodeHerd(input);

        result.Should().Be(expected);
    }
}