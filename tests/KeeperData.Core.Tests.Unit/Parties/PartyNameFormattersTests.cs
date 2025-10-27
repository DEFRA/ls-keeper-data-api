using FluentAssertions;
using KeeperData.Core.Domain.Parties.Formatters;

namespace KeeperData.Core.Tests.Unit.Parties;

public class PartyNameFormattersTests
{
    [Theory]
    [InlineData("John", "Paul", "John Paul")]
    [InlineData("John", null, "John")]
    [InlineData(null, "Paul", "Paul")]
    [InlineData(null, null, "")]
    [InlineData("  ", "Paul", "Paul")]
    [InlineData("John", "  ", "John")]
    public void FormatPartyFirstName_ShouldJoinGivenNames(string? givenName, string? givenName2, string expected)
    {
        var result = PartyNameFormatters.FormatPartyFirstName(givenName, givenName2);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("Acme Ltd", "Mr", "John", "Paul", "Smith", "Acme Ltd")]
    [InlineData(null, "Mr", "John", "Paul", "Smith", "Mr John Paul Smith")]
    [InlineData(null, null, "John", null, "Smith", "John Smith")]
    [InlineData(null, null, null, null, null, "")]
    [InlineData(null, null, "  ", null, "Smith", "Smith")]
    public void FormatPartyFullName_ShouldUseOrganisationOrJoinParts(
        string? organisationName,
        string? title,
        string? givenName,
        string? givenName2,
        string? familyName,
        string expected)
    {
        var result = PartyNameFormatters.FormatPartyFullName(organisationName, title, givenName, givenName2, familyName);
        result.Should().Be(expected);
    }
}