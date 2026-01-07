using FluentAssertions;
using KeeperData.Application.Configuration;
using KeeperData.Application.Queries.Parties;

namespace KeeperData.Application.Tests.Unit.Queries.Parties;

public class GetPartiesQueryValidatorTests
{
    [Theory]
    [InlineData(1, 10, "test@test.com", "asc", "name", "John", true)]
    [InlineData(0, null, null, null, null, null, false)]
    [InlineData(null, 0, null, null, null, null, false)]
    [InlineData(null, 101, null, null, null, null, false)]
    [InlineData(1, 1, null, null, null, null, true)]
    [InlineData(1, 100, null, null, null, null, true)]
    [InlineData(null, null, "a@b.com", null, null, null, true)]
    [InlineData(null, null, "", null, null, null, false)]
    [InlineData(null, null, null, "asc", "type", null, true)]
    [InlineData(null, null, null, "desc", "type", null, true)]
    [InlineData(null, null, null, "other", "type", null, false)]
    [InlineData(null, null, null, "invalid", "type", null, false)]
    [InlineData(null, null, null, "asc", null, null, false)]
    public void ShouldValidateQueryParametersCorrectly(int? page, int? pageSize, string? email, string? sort, string? order, string? firstName, bool expectedIsValid)
    {
        var query = new GetPartiesQuery() { Page = page ?? 1, PageSize = pageSize ?? 10, Email = email, Sort = sort, Order = order, FirstName = firstName };
        var sut = new GetPartiesQueryValidator(new QueryValidationConfig() { MaxPageSize = 100 });
        var result = sut.Validate(query);
        result.IsValid.Should().Be(expectedIsValid);
    }

    [Fact]
    public void WhenQueryingWithReducedMaxPageSize_HigherSizeShouldFail()
    {
        var query = new GetPartiesQuery() { Page = 1, PageSize = 7 };
        var sut = new GetPartiesQueryValidator(new QueryValidationConfig() { MaxPageSize = 6 });
        var result = sut.Validate(query);
        result.Errors.Count.Should().Be(1);
    }

    [Fact]
    public void GetPartyByIdQueryValidator_ValidatesCorrectly()
    {
        var validator = new GetPartyByIdQueryValidator();
        validator.Validate(new GetPartyByIdQuery("123")).IsValid.Should().BeTrue();
        validator.Validate(new GetPartyByIdQuery("")).IsValid.Should().BeFalse();
    }
}