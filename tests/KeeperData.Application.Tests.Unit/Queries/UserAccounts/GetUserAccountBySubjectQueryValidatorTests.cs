using FluentAssertions;
using KeeperData.Application.Queries.UserAccounts;

namespace KeeperData.Application.Tests.Unit.Queries.UserAccounts;

public class GetUserAccountBySubjectQueryValidatorTests
{
    private readonly GetUserAccountBySubjectQueryValidator _sut = new();

    [Fact]
    public void GivenAValidSubject_WhenValidating_ThenResultIsValid()
    {
        var result = _sut.Validate(new GetUserAccountBySubjectQuery("9f3a1c2e-0b6d-4f4e-9d2a-7c8b1e5f0a3d"));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void GivenAnEmptySubject_WhenValidating_ThenResultIsInvalid()
    {
        var result = _sut.Validate(new GetUserAccountBySubjectQuery(""));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void GivenASubjectExceedingMaxLength_WhenValidating_ThenResultIsInvalid()
    {
        var subject = new string('a', 257);

        var result = _sut.Validate(new GetUserAccountBySubjectQuery(subject));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void GivenASubjectAtMaxLength_WhenValidating_ThenResultIsValid()
    {
        var subject = new string('a', 256);

        var result = _sut.Validate(new GetUserAccountBySubjectQuery(subject));

        result.IsValid.Should().BeTrue();
    }
}