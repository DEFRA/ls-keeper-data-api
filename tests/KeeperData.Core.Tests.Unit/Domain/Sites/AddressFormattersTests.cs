using FluentAssertions;
using KeeperData.Core.Domain.Sites.Formatters;

namespace KeeperData.Core.Tests.Unit.Domain.Sites;

public class AddressFormattersTests
{
    [Theory]
    [InlineData((short)1, (short)3, (short)10, (short)12, null, null, "1-3, 10-12")]
    [InlineData((short)2, null, (short)56, null, null, null, "2, 56")]
    [InlineData(null, null, (short)100, (short)102, null, null, "100-102")]
    [InlineData(null, null, null, null, null, null, "")]
    [InlineData(null, null, null, null, "Flat A", "Building B", "Flat A, Building B")]
    [InlineData((short)1, (short)3, (short)10, (short)12, "Flat A", null, "Flat A, 10-12")]
    [InlineData(null, null, null, null, "The Estate Office", null, "The Estate Office")]
    [InlineData(null, null, null, null, null, "The Estate Office", "The Estate Office")]
    public void GivenPaonAndSaonAddressElements_WhenCombining_ShouldFormatAddress(
        short? saonStart, short? saonEnd,
        short? paonStart, short? paonEnd,
        string? saonDescription, string? paonDescription,
        string expectedResult)
    {
        var result = AddressFormatters.FormatAddressRange(
            saonStart, saonEnd,
            paonStart, paonEnd,
            saonDescription,
            paonDescription);

        result.Should().Be(expectedResult);
    }

    [Theory]
    [InlineData((short)1, 'A', (short)3, 'B', (short)10, 'C', (short)12, 'D', null, null, "1A-3B, 10C-12D")]
    [InlineData((short)2, null, null, null, (short)56, null, null, null, null, null, "2, 56")]
    [InlineData(null, null, null, null, (short)100, null, (short)102, null, null, null, "100-102")]
    [InlineData(null, null, null, null, null, null, null, null, null, null, "")]
    [InlineData(null, null, null, null, null, null, null, null, "Flat A", "Building B", "Flat A, Building B")]
    [InlineData((short)1, 'A', (short)3, 'B', (short)10, 'C', (short)12, 'D', "Flat A", null, "Flat A, 10C-12D")]
    [InlineData(null, null, null, null, null, null, null, null, "The Estate Office", null, "The Estate Office")]
    [InlineData(null, null, null, null, null, null, null, null, null, "The Estate Office", "The Estate Office")]
    public void GivenPaonAndSaonAddressElementsIncSuffix_WhenCombining_ShouldFormatAddress(
        short? saonStart, char? saonStartSuffix,
        short? saonEnd, char? saonEndSuffix,
        short? paonStart, char? paonStartSuffix,
        short? paonEnd, char? paonEndSuffix,
        string? saonDescription,
        string? paonDescription,
        string expectedResult)
    {
        var result = AddressFormatters.FormatAddressRange(
            saonStart, saonStartSuffix,
            saonEnd, saonEndSuffix,
            paonStart, paonStartSuffix,
            paonEnd, paonEndSuffix,
            saonDescription,
            paonDescription);

        result.Should().Be(expectedResult);
    }
}