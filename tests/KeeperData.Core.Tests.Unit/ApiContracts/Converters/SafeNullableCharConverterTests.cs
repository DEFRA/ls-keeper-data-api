using FluentAssertions;
using KeeperData.Infrastructure;
using System.Text.Json;

namespace KeeperData.Core.Tests.Unit.ApiContracts.Converters;

public class SafeNullableCharConverterTests
{
    public static TheoryData<string, char?> CharTestCases => new()
    {
        { "\"A\"", 'A' },
        { "\"\"", null },
        { "\"AB\"", null }
    };

    [Theory]
    [MemberData(nameof(CharTestCases))]
    public void Read_ValidAndInvalidCharStrings_ReturnsExpected(string json, char? expected)
    {
        var result = JsonSerializer.Deserialize<char?>(json, JsonDefaults.DefaultOptionsWithDataBridgeApiSupport);

        result.Should().Be(expected);
    }
}