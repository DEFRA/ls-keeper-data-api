using FluentAssertions;
using KeeperData.Infrastructure;
using System.Text.Json;

namespace KeeperData.Core.Tests.Unit.ApiContracts.Converters;

public class SafeNullableIntConverterTests
{
    public static TheoryData<string, int?> IntTestCases => new()
    {
        { "\"123\"", 123 },
        { "\"\"", null },
        { "\"abc\"", null }
    };

    [Theory]
    [MemberData(nameof(IntTestCases))]
    public void Read_ValidAndInvalidIntStrings_ReturnsExpected(string json, int? expected)
    {
        var result = JsonSerializer.Deserialize<int?>(json, JsonDefaults.DefaultOptionsWithDataBridgeApiSupport);

        result.Should().Be(expected);
    }
}
