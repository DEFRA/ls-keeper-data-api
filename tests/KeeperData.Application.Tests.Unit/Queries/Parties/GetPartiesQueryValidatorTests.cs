using FluentAssertions;
using KeeperData.Application.Configuration;
using KeeperData.Application.Queries.Parties;

namespace KeeperData.Application.Tests.Unit.Queries.Parties;

public class GetPartiesQueryValidatorTests
{
    [Theory]
    [InlineData(0, null, null, null, null, 1)]
    [InlineData(null, 0, null, null, null, 1)]
    [InlineData(null, 101, null, null, null, 1)]
    [InlineData(1, 1, null, null, null, 0)]
    [InlineData(1, 100, null, null, null, 0)]
    [InlineData(null, null, "a@b.com", null, null, 0)]
    [InlineData(null, null, "", null, null, 1)]
    [InlineData(null, null, null, "asc", "type", 0)]
    [InlineData(null, null, null, "desc", "type", 0)]
    [InlineData(null, null, null, "other", "type", 1)]
    public void QueryWithInvalidParameters_ShouldFail(int? page, int? pageSize, string? email, string? sort, string? order, int numberOfErrors)
    {
        var query = new GetPartiesQuery() { Page = page ?? 1, PageSize = pageSize ?? 10, Email = email, Sort = sort, Order = order };
        var sut = new GetPartiesQueryValidator(new QueryValidationConfig<GetPartiesQueryValidator>() { MaxPageSize = 100 });
        var result = sut.Validate(query);
        result.Errors.Count.Should().Be(numberOfErrors);
    }

    [Fact]
    public void WhenQueryingWithReducedMaxPageSize_HigherSizeShouldFail()
    {
        var query = new GetPartiesQuery() { Page = 1, PageSize = 7 };
        var sut = new GetPartiesQueryValidator(new QueryValidationConfig<GetPartiesQueryValidator>() { MaxPageSize = 6 });
        var result = sut.Validate(query);
        result.Errors.Count.Should().Be(1);
    }
}