using FluentAssertions;
using KeeperData.Core.Domain.Sites.Formatters;

namespace KeeperData.Core.Tests.Unit.Sites;

public class TestingIntervalFormattersTests
{
    [Fact]
    public void GivenValidIntervalAndUnit_WhenFormatted_ThenReturnsCombinedTitleCase()
    {
        decimal? interval = 6;
        var unit = "months";

        var result = TestingIntervalFormatters.FormatTbTestingInterval(interval, unit);

        result.Should().Be("6 Months");
    }

    [Fact]
    public void GivenValidIntervalAndNullUnit_WhenFormatted_ThenReturnsIntervalOnly()
    {
        decimal? interval = 3;
        string? unit = null;

        var result = TestingIntervalFormatters.FormatTbTestingInterval(interval, unit);

        result.Should().Be("3");
    }

    [Fact]
    public void GivenValidIntervalAndWhitespaceUnit_WhenFormatted_ThenReturnsIntervalOnly()
    {
        decimal? interval = 2;
        var unit = "   ";

        var result = TestingIntervalFormatters.FormatTbTestingInterval(interval, unit);

        result.Should().Be("2");
    }

    [Fact]
    public void GivenNullInterval_WhenFormatted_ThenReturnsNull()
    {
        decimal? interval = null;
        var unit = "weeks";

        var result = TestingIntervalFormatters.FormatTbTestingInterval(interval, unit);

        result.Should().BeNull();
    }

    [Fact]
    public void GivenValidIntervalAndMixedCaseUnit_WhenFormatted_ThenReturnsNormalizedTitleCase()
    {
        decimal? interval = 12;
        var unit = "  dAyS ";

        var result = TestingIntervalFormatters.FormatTbTestingInterval(interval, unit);

        result.Should().Be("12 Days");
    }
}