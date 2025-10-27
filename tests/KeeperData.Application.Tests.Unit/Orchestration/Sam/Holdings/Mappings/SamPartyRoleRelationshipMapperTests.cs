using FluentAssertions;
using KeeperData.Application.Orchestration.Sam.Holdings.Mappings;
using KeeperData.Core.ApiClients.DataBridgeApi;
using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Core.Services;
using KeeperData.Tests.Common.Factories;
using KeeperData.Tests.Common.Mappings;
using Moq;

namespace KeeperData.Application.Tests.Unit.Orchestration.Sam.Holdings.Mappings;

public class SamPartyRoleRelationshipMapperTests
{
    private readonly Mock<IRoleTypeLookupService> _roleTypeLookupServiceMock = new();
    private readonly Mock<ICountryIdentifierLookupService> _countryIdentifierLookupServiceMock = new();

    private readonly Func<string?, CancellationToken, Task<(string?, string?)>> _resolveRoleType;
    private readonly Func<string?, CancellationToken, Task<(string?, string?)>> _resolveCountry;

    public SamPartyRoleRelationshipMapperTests()
    {
        _roleTypeLookupServiceMock
            .Setup(x => x.FindAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string? input, CancellationToken token) => (Guid.NewGuid().ToString(), input));

        _countryIdentifierLookupServiceMock
            .Setup(x => x.FindAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string? input, CancellationToken token) => (Guid.NewGuid().ToString(), input));

        _resolveRoleType = _roleTypeLookupServiceMock.Object.FindAsync;
        _resolveCountry = _countryIdentifierLookupServiceMock.Object.FindAsync;
    }

    [Fact]
    public void GivenNullableParties_WhenCallingToSilver_ShouldReturnEmptyList()
    {
        var results = SamPartyRoleRelationshipMapper.ToSilver(null!,
            Guid.NewGuid().ToString(),
            Guid.NewGuid().ToString());

        results.Should().NotBeNull();
        results.Count.Should().Be(0);
    }

    [Fact]
    public void GivenEmptyParties_WhenCallingToSilver_ShouldReturnEmptyList()
    {
        var results = SamPartyRoleRelationshipMapper.ToSilver([],
            Guid.NewGuid().ToString(),
            Guid.NewGuid().ToString());

        results.Should().NotBeNull();
        results.Count.Should().Be(0);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public async Task GivenPartiesExist_WhenCallingToSilver_ShouldReturnPopulatedList(int quantity)
    {
        var records = GenerateSamParty(quantity);

        var holdingIdentifier = Guid.NewGuid().ToString();
        var holdingIdentifierType = Guid.NewGuid().ToString();

        var silverParties = await SamPartyMapper.ToSilver(records,
            holdingIdentifier,
            _resolveRoleType,
            _resolveCountry,
            CancellationToken.None);

        var results = SamPartyRoleRelationshipMapper.ToSilver(silverParties,
            holdingIdentifier,
            holdingIdentifierType);

        foreach (var party in silverParties)
        {
            party.Should().NotBeNull();
            party.Roles.Should().NotBeNull();

            foreach (var role in party.Roles)
            {
                var mapped = results.Single(r => r.Id == role.IdentifierId);
                VerifySamPartyRoleRelationshipMappings.VerifyMapping_From_SamPartyDocument_To_PartyRoleRelationshipDocument(
                    party,
                    mapped,
                    holdingIdentifier,
                    holdingIdentifierType);
            }
        }
    }

    private static List<SamParty> GenerateSamParty(int quantity)
    {
        var factory = new MockSamRawDataFactory();

        var partyIds = Enumerable.Range(0, quantity)
            .Select(_ => Guid.NewGuid().ToString())
            .ToList();

        var records = Enumerable.Range(0, quantity)
            .Select(_ => factory.CreateMockParty(
                changeType: DataBridgeConstants.ChangeTypeInsert,
                batchId: 1,
                partyIds: partyIds))
            .ToList();

        return records;
    }
}