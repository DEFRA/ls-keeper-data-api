using FluentAssertions;
using KeeperData.Core.Domain.Sites.Extensions;

namespace KeeperData.Core.Tests.Unit.Extensions;

public class FormatAddressExtensionsTests
{
    [Theory]
    [InlineData((short)1, (short)3, (short)10, (short)12, "1-3, 10-12")]
    [InlineData((short)2, null, (short)56, null, "2, 56")]
    [InlineData(null, null, (short)100, (short)102, "100-102")]
    [InlineData(null, null, null, null, "")]
    public void GivenPaonAndSaonAddressElements_WhenCombining_ShouldFormatAddress(
        short? saonStart,
        short? saonEnd,
        short? paonStart,
        short? paonEnd,
        string expectedResult)
    {
        var result = FormatAddressExtensions.FormatAddressRange(saonStart, saonEnd, paonStart, paonEnd);
        result.Should().Be(expectedResult);
    }

    [Theory]
    [InlineData((short)1, 'A', (short)3, 'B', (short)10, 'C', (short)12, 'D', "Unit 1A-3B, 10C-12D")]
    [InlineData((short)2, null, null, null, (short)56, null, null, null, "Unit 2, 56")]
    [InlineData(null, null, null, null, (short)100, null, (short)102, null, "100-102")]
    [InlineData(null, null, null, null, null, null, null, null, "")]
    public void GivenPaonAndSaonAddressElementsIncSuffix_WhenCombining_ShouldFormatAddress(
        short? saonStart, char? saonStartSuffix,
        short? saonEnd, char? saonEndSuffix,
        short? paonStart, char? paonStartSuffix,
        short? paonEnd, char? paonEndSuffix,
        string expectedResult)
    {
        var result = FormatAddressExtensions.FormatAddressRange(
            saonStart, saonStartSuffix,
            saonEnd, saonEndSuffix,
            paonStart, paonStartSuffix,
            paonEnd, paonEndSuffix,
            saonLabel: "Unit");

        result.Should().Be(expectedResult);
    }
}
