using FluentAssertions;
using KeeperData.Infrastructure;
using System.Text.Json;

namespace KeeperData.Core.Tests.Unit.ApiContracts.Converters;

public class SafeNullableShortConverterTests
{
    public static TheoryData<string, short?> ShortTestCases => new()
    {
        { "\"123\"", (short)123 },
        { "\"\"", null },
        { "\"not-a-number\"", null }
    };

    [Theory]
    [MemberData(nameof(ShortTestCases))]
    public void Read_ValidAndInvalidShortStrings_ReturnsExpected(string json, short? expected)
    {
        var result = JsonSerializer.Deserialize<short?>(json, JsonDefaults.DefaultOptionsWithDataBridgeApiSupport);

        result.Should().Be(expected);
    }
}