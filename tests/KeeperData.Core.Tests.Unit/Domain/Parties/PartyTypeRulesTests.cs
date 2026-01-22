using FluentAssertions;
using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Core.Domain.Enums;
using KeeperData.Core.Domain.Parties.Rules;

namespace KeeperData.Core.Tests.Unit.Parties;

public class PartyTypeRulesTests
{
    [Theory]
    [InlineData("Smith", "Mr", PartyType.Person)]
    [InlineData("Smith", null, PartyType.Business)]
    [InlineData(null, "Miss", PartyType.Business)]
    [InlineData(null, null, PartyType.Business)]
    [InlineData(" ", "Mrs", PartyType.Business)]
    [InlineData("Smith", " ", PartyType.Business)]
    public void DeterminePartyType_CtsAgentOrKeeper_ShouldReturnExpectedType(
        string? surname, string? title, PartyType expected)
    {
        var party = new CtsAgentOrKeeper
        {
            PAR_SURNAME = surname,
            PAR_TITLE = title
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