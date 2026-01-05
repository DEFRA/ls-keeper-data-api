using FluentAssertions;
using KeeperData.Application.Configuration;
using KeeperData.Application.Queries.Parties;
using KeeperData.Application.Queries.Sites;
using Xunit;

namespace KeeperData.Application.Tests.Unit.Validators;

public class ValidatorTests
{
    private readonly QueryValidationConfig _validationConfig = new() { MaxPageSize = 100 };

    [Fact]
    public void GetPartiesQueryValidator_ValidatesCorrectly()
    {
        var validator = new GetPartiesQueryValidator(_validationConfig);

        var validQuery = new GetPartiesQuery { Page = 1, PageSize = 10, Sort = "asc", Order = "name", FirstName = "John", Email = "test@test.com" };
        validator.Validate(validQuery).IsValid.Should().BeTrue();

        var invalidPage = new GetPartiesQuery { Page = 0 };
        validator.Validate(invalidPage).IsValid.Should().BeFalse();

        var invalidSort = new GetPartiesQuery { Sort = "invalid" };
        validator.Validate(invalidSort).IsValid.Should().BeFalse();

        var missingOrder = new GetPartiesQuery { Sort = "asc" }; // Order is required if Sort is present
        validator.Validate(missingOrder).IsValid.Should().BeFalse();
    }

    [Fact]
    public void GetPartyByIdQueryValidator_ValidatesCorrectly()
    {
        var validator = new GetPartyByIdQueryValidator();

        validator.Validate(new GetPartyByIdQuery("123")).IsValid.Should().BeTrue();
        validator.Validate(new GetPartyByIdQuery("")).IsValid.Should().BeFalse();
    }

    [Fact]
    public void GetSitesQueryValidator_ValidatesCorrectly()
    {
        var validator = new GetSitesQueryValidator(_validationConfig);

        var validQuery = new GetSitesQuery { Page = 1, PageSize = 10, Sort = "asc", Order = "name" };
        validator.Validate(validQuery).IsValid.Should().BeTrue();

        var invalidPage = new GetSitesQuery { Page = 0 };
        validator.Validate(invalidPage).IsValid.Should().BeFalse();

        var invalidSort = new GetSitesQuery { Sort = "invalid" };
        validator.Validate(invalidSort).IsValid.Should().BeFalse();
    }

    [Fact]
    public void GetSiteByIdQueryValidator_ValidatesCorrectly()
    {
        var validator = new GetSiteByIdQueryValidator();

        validator.Validate(new GetSiteByIdQuery("123")).IsValid.Should().BeTrue();
        validator.Validate(new GetSiteByIdQuery("")).IsValid.Should().BeFalse();
    }
}