using FluentAssertions;
using KeeperData.Core.Domain.Parties.Formatters;

namespace KeeperData.Core.Tests.Unit.Domain.Parties;

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
    [InlineData("Acme Ltd", "Mr", "John", "Paul", "JP", "Smith", "Acme Ltd")]
    [InlineData(null, "Mr", "John", "Paul", "JP", "Smith", "Mr John Paul JP Smith")]
    [InlineData(null, "Ms", "Anna", null, "A", "Taylor", "Ms Anna A Taylor")]
    [InlineData(null, null, "John", null, "J", "Smith", "John J Smith")]
    [InlineData(null, null, null, null, "X", "Smith", "X Smith")]
    [InlineData(null, null, null, null, null, "Smith", "Smith")]
    [InlineData(null, null, null, null, null, null, "")]
    [InlineData(null, null, "  ", null, null, "Smith", "Smith")]
    public void FormatPartyFullName_ShouldUseOrganisationOrJoinParts(
        string? organisationName,
        string? title,
        string? givenName,
        string? givenName2,
        string? initials,
        string? familyName,
        string expected)
    {
        var result = PartyNameFormatters.FormatPartyFullName(organisationName, title, givenName, givenName2, initials, familyName);
        result.Should().Be(expected);
    }
}