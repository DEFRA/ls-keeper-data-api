using FluentAssertions;
using KeeperData.Application.Configuration;
using KeeperData.Application.Queries.Countries;

namespace KeeperData.Application.Tests.Unit.Queries.Countries;

public class GetCountriesQueryValidatorTests
{
    [Theory]
    [InlineData(0, null, null, null, null, 1)]
    [InlineData(null, 0, null, null, null, 1)]
    [InlineData(null, 101, null, null, null, 1)]
    [InlineData(1, 1, null, null, null, 0)]
    [InlineData(1, 100, null, null, null, 0)]
    [InlineData(null, null, "a,b,c", null, null, 0)]
    [InlineData(null, null, "a,,c", null, null, 1)]
    [InlineData(null, null, null, "asc", "name", 0)]
    [InlineData(null, null, null, "desc", "name", 0)]
    [InlineData(null, null, null, "other", "name", 1)]
    public void QueryWithInvalidParameters_ShouldFail(int? page, int? pageSize, string? codeCsv, string? sort, string? order, int numberOfErrors)
    {
        var query = new GetCountriesQuery() { Page = page ?? 1, PageSize = pageSize ?? 10, Code = codeCsv?.Split(',').ToList(), Sort = sort, Order = order };
        var sut = new GetCountriesQueryValidator(new QueryValidationConfig() { MaxPageSize = 100 });
        var result = sut.Validate(query);
        result.Errors.Count.Should().Be(numberOfErrors);
    }

    [Fact]
    public void WhenQueryingWithReducedMaxPageSize_HigherSizeShouldFail()
    {
        var query = new GetCountriesQuery() { Page = 1, PageSize = 7 };
        var sut = new GetCountriesQueryValidator(new QueryValidationConfig() { MaxPageSize = 6 });
        var result = sut.Validate(query);
        result.Errors.Count.Should().Be(1);
    }
}