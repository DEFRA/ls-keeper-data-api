using FluentAssertions;
using KeeperData.Application.Configuration;
using KeeperData.Application.Queries.Sites;

namespace KeeperData.Application.Tests.Unit.Queries.Sites;

public class GetSitesQueryValidatorTests
{
    [Theory]
    [InlineData(1, 10, null, null, "asc", "name", true)]
    [InlineData(0, null, null, null, null, null, false)]
    [InlineData(null, 0, null, null, null, null, false)]
    [InlineData(null, 101, null, null, null, null, false)]
    [InlineData(1, 1, null, null, null, null, true)]
    [InlineData(1, 100, null, null, null, null, true)]
    [InlineData(null, null, "a,b,c", null, null, null, true)]
    [InlineData(null, null, "a,,c", null, null, null, false)]
    [InlineData(null, null, null, "x,y,z", null, null, true)]
    [InlineData(null, null, null, "x,,z", null, null, false)]
    [InlineData(null, null, null, null, "asc", "type", true)]
    [InlineData(null, null, null, null, "desc", "type", true)]
    [InlineData(null, null, null, null, "invalid", "type", false)]
    public void QueryWithInvalidParameters_ShouldFail(int? page, int? pageSize, string? typeCsv, string? idsCsv, string? sort, string? order, bool expectedIsValid)
    {
        var query = new GetSitesQuery()
        {
            Page = page ?? 1,
            PageSize = pageSize ?? 10,
            Type = typeCsv?.Split(',').ToList(),
            SiteIdentifiers = idsCsv?.Split(',').ToList(),
            Sort = sort,
            Order = order
        };
        var sut = new GetSitesQueryValidator(new QueryValidationConfig<GetSitesQueryValidator>() { MaxPageSize = 100 });
        var result = sut.Validate(query);
        result.IsValid.Should().Be(expectedIsValid);
    }

    [Fact]
    public void WhenQueryingWithReducedMaxPageSize_HigherSizeShouldFail()
    {
        var query = new GetSitesQuery() { Page = 1, PageSize = 7 };
        var sut = new GetSitesQueryValidator(new QueryValidationConfig<GetSitesQueryValidator>() { MaxPageSize = 6 });
        var result = sut.Validate(query);
        result.Errors.Count.Should().Be(1);
    }

    [Theory]
    [InlineData(4, 3, 1)]
    [InlineData(5, 5, 0)]
    public void WhenQueryingWithReducedMaxSiteTypes_ShouldFailOnlyIfMaxExceeded(int numberInQuery, int maxAllowed, int expectedNumberOfErrors)
    {
        var query = new GetSitesQuery() { Type = Enumerable.Range(1, numberInQuery).Select(x => x.ToString()).ToList() };
        var sut = new GetSitesQueryValidator(new QueryValidationConfig<GetSitesQueryValidator>() { MaxQueryableTypes = maxAllowed });
        var result = sut.Validate(query);
        result.Errors.Count.Should().Be(expectedNumberOfErrors);
    }

    [Theory]
    [InlineData(4, 3, 1)]
    [InlineData(5, 5, 0)]
    public void WhenQueryingWithReducedMaxSiteIdentifiers_ShouldFailOnlyIfMaxExceeded(int numberInQuery, int maxAllowed, int expectedNumberOfErrors)
    {
        var query = new GetSitesQuery() { SiteIdentifiers = Enumerable.Range(1, numberInQuery).Select(x => x.ToString()).ToList() };
        var sut = new GetSitesQueryValidator(new QueryValidationConfig<GetSitesQueryValidator>() { MaxQueryableTypes = maxAllowed });
        var result = sut.Validate(query);
        result.Errors.Count.Should().Be(expectedNumberOfErrors);
    }

    [Fact]
    public void GetSiteByIdQueryValidator_ValidatesCorrectly()
    {
        var validator = new GetSiteByIdQueryValidator();
        validator.Validate(new GetSiteByIdQuery("123")).IsValid.Should().BeTrue();
        validator.Validate(new GetSiteByIdQuery("")).IsValid.Should().BeFalse();
    }
}