using FluentAssertions;
using KeeperData.Core.Domain.Sites.Formatters;

namespace KeeperData.Core.Tests.Unit.Domain.Sites;

public class HoldingIdentifierFormattersTests
{
    [Theory]
    [InlineData("12/345/6789/01", "12/345/6789")]
    [InlineData("AB/CDE/FGHI/99", "AB/CDE/FGHI")]
    [InlineData("XX/XXX/XXXX/YY", "XX/XXX/XXXX")]
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

    [Theory]
    [InlineData("AG-12/345/6789", "12/345/6789")]
    [InlineData("AH-AB/CDE/FGHI", "AB/CDE/FGHI")]
    [InlineData("XX-XX/XXX/XXXX", "XX/XXX/XXXX")]
    [InlineData("AG-3000123", "3000123")]
    [InlineData("XX-3000123", "3000123")]
    public void LidIdentifierToCph_ValidCphh_TrimsSuffix(string input, string expected)
    {
        var result = input.LidIdentifierToCph();
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("12/345/6789")]
    [InlineData("AB/CDE/FGHI")]
    [InlineData("3000123")]
    public void LidIdentifierToCph_AlreadyCph_ReturnsUnchanged(string input)
    {
        var result = input.LidIdentifierToCph();
        result.Should().Be(input);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void LidIdentifierToCph_NullOrWhitespace_ReturnsEmptyString(string? input)
    {
        var result = input.LidIdentifierToCph();
        result.Should().Be(string.Empty);
    }
}