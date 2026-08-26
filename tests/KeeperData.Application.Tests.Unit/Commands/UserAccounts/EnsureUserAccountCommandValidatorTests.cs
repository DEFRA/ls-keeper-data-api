using FluentAssertions;
using KeeperData.Application.Commands.UserAccounts;

namespace KeeperData.Application.Tests.Unit.Commands.UserAccounts;

public class EnsureUserAccountCommandValidatorTests
{
    private readonly EnsureUserAccountCommandValidator _sut = new();

    [Fact]
    public void GivenAllClaims_WhenValidating_ThenTheCommandIsValid()
    {
        var result = _sut.Validate(new EnsureUserAccountCommand("subject", "jane.farmer@example.com", "Jane", "Farmer"));

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("", "jane.farmer@example.com", "Jane", "Farmer")]
    [InlineData("subject", "", "Jane", "Farmer")]
    [InlineData("subject", "not-an-email", "Jane", "Farmer")]
    [InlineData("subject", "jane.farmer@example.com", "", "Farmer")]
    [InlineData("subject", "jane.farmer@example.com", "Jane", "")]
    public void GivenAMissingOrInvalidClaim_WhenValidating_ThenTheCommandIsInvalid(
        string subject, string email, string givenName, string familyName)
    {
        var result = _sut.Validate(new EnsureUserAccountCommand(subject, email, givenName, familyName));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void GivenASubjectLongerThanTheMaximum_WhenValidating_ThenTheCommandIsInvalid()
    {
        var result = _sut.Validate(new EnsureUserAccountCommand(new string('s', 257), "jane.farmer@example.com", "Jane", "Farmer"));

        result.IsValid.Should().BeFalse();
    }
}