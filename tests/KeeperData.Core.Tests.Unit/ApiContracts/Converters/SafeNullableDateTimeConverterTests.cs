using FluentAssertions;
using KeeperData.Infrastructure;
using System.Text.Json;

namespace KeeperData.Core.Tests.Unit.ApiContracts.Converters;

public class SafeNullableDateTimeConverterTests
{
    public static TheoryData<string, DateTime?> DateTimeTestCases => new()
    {
        { "\"2023-11-10T14:22:00Z\"", DateTime.Parse("2023-11-10T14:22:00Z") },
        { "\"\"", null },
        { "\"not-a-date\"", null }
    };

    [Theory]
    [MemberData(nameof(DateTimeTestCases))]
    public void Read_ValidAndInvalidDateStrings_ReturnsExpected(string json, DateTime? expected)
    {
        var result = JsonSerializer.Deserialize<DateTime?>(json, JsonDefaults.DefaultOptionsWithDataBridgeApiSupport);

        result.Should().Be(expected);
    }
}