using FluentAssertions;
using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Core.Domain.Enums;
using KeeperData.Core.Domain.Parties.Rules;

namespace KeeperData.Core.Tests.Unit.Parties;

public class PartyTypeRulesTests
{
    [Theory]
    [InlineData("Smith", "J", PartyType.Person)]
    [InlineData("Smith", null, PartyType.Business)]
    [InlineData(null, "J", PartyType.Business)]
    [InlineData(null, null, PartyType.Business)]
    [InlineData(" ", "J", PartyType.Business)]
    [InlineData("Smith", " ", PartyType.Business)]
    public void DeterminePartyType_CtsAgentOrKeeper_ShouldReturnExpectedType(
        string? surname, string? initials, PartyType expected)
    {
        var party = new CtsAgentOrKeeper
        {
            PAR_SURNAME = surname,
            PAR_INITIALS = initials
        };

        var result = party.DeterminePartyType();
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("Acme Ltd", PartyType.Business)]
    [InlineData(" ", PartyType.Person)]
    [InlineData(null, PartyType.Person)]
    public void DeterminePartyType_SamCphHolder_ShouldReturnExpectedType(
        string? organisationName, PartyType expected)
    {
        var holder = new SamCphHolder
        {
            ORGANISATION_NAME = organisationName
        };

        var result = holder.DeterminePartyType();
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("Acme Ltd", PartyType.Business)]
    [InlineData(" ", PartyType.Person)]
    [InlineData(null, PartyType.Person)]
    public void DeterminePartyType_SamParty_ShouldReturnExpectedType(
        string? organisationName, PartyType expected)
    {
        var party = new SamParty
        {
            ORGANISATION_NAME = organisationName
        };

        var result = party.DeterminePartyType();
        result.Should().Be(expected);
    }
}
