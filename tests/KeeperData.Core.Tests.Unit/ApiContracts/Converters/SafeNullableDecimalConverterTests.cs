using FluentAssertions;
using KeeperData.Infrastructure;
using System.Text.Json;

namespace KeeperData.Core.Tests.Unit.ApiContracts.Converters;

public class SafeNullableDecimalConverterTests
{
    public static TheoryData<string, decimal?> DecimalTestCases => new()
    {
        { "\"123.45\"", 123.45m },
        { "\"\"", null },
        { "\"not-a-number\"", null }
    };

    [Theory]
    [MemberData(nameof(DecimalTestCases))]
    public void Read_ValidAndInvalidDecimalStrings_ReturnsExpected(string json, decimal? expected)
    {
        var result = JsonSerializer.Deserialize<decimal?>(json, JsonDefaults.DefaultOptionsWithDataBridgeApiSupport);

        result.Should().Be(expected);
    }
}