using FluentAssertions;
using KeeperData.Infrastructure;
using System.Text.Json;

namespace KeeperData.Core.Tests.Unit.ApiContracts.Converters;

public class SafeNullableBoolConverterTests
{
    public static TheoryData<string, bool?> BoolTestCases => new()
    {
        { "\"true\"", true },
        { "\"false\"", false },
        { "\"1\"", true },
        { "\"0\"", false },
        { "\"yes\"", null },
        { "\"\"", null }
    };

    [Theory]
    [MemberData(nameof(BoolTestCases))]
    public void Read_ValidAndInvalidBoolStrings_ReturnsExpected(string json, bool? expected)
    {
        var result = JsonSerializer.Deserialize<bool?>(json, JsonDefaults.DefaultOptionsWithDataBridgeApiSupport);

        result.Should().Be(expected);
    }
}