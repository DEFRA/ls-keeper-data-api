using FluentAssertions;
using KeeperData.Core.Domain.Sites.Formatters;

namespace KeeperData.Core.Tests.Unit.Sites;

public class HoldingIdentifierFormattersTests
{
    [Theory]
    [InlineData("12/345/6789-01", "12/345/6789")]
    [InlineData("AB/CDE/FGHI-99", "AB/CDE/FGHI")]
    [InlineData("XX/XXX/XXXX-YY", "XX/XXX/XXXX")]
    public void CphhToCph_ValidCphh_TrimsSuffix(string input, string expected)
    {
        var result = input.CphhToCph();
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("12/345/6789")]
    [InlineData("AB/CDE/FGHI")]
    public void CphhToCph_AlreadyCph_ReturnsUnchanged(string input)
    {
        var result = input.CphhToCph();
        result.Should().Be(input);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CphhToCph_NullOrWhitespace_ReturnsEmptyString(string? input)
    {
        var result = input.CphhToCph();
        result.Should().Be(string.Empty);
    }
}