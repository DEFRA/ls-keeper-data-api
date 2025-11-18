using FluentAssertions;
using KeeperData.Application.Orchestration.Imports.Cts.Mappings;
using KeeperData.Core.ApiClients.DataBridgeApi;
using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Core.Domain.Enums;
using KeeperData.Core.Domain.Sites.Formatters;
using KeeperData.Core.Services;
using KeeperData.Tests.Common.Factories;
using KeeperData.Tests.Common.Generators;
using KeeperData.Tests.Common.Mappings;
using Moq;

namespace KeeperData.Application.Tests.Unit.Orchestration.Imports.Cts.Holdings.Mapping;

public class CtsPartyRoleRelationshipMapperTests
{
    private readonly Mock<IRoleTypeLookupService> _roleTypeLookupServiceMock = new();
    private readonly Func<string?, CancellationToken, Task<(string?, string?)>> _resolveRoleType;

    public CtsPartyRoleRelationshipMapperTests()
    {
        _roleTypeLookupServiceMock
            .Setup(x => x.FindAsync("AGENT", It.IsAny<CancellationToken>()))
            .ReturnsAsync(("8184ae3d-c3c4-4904-b1b8-539eeadbf245", "Agent"));

        _roleTypeLookupServiceMock
            .Setup(x => x.FindAsync("LIVESTOCKKEEPER", It.IsAny<CancellationToken>()))
            .ReturnsAsync(("b2637b72-2196-4a19-bdf0-85c7ff66cf60", "Livestock Keeper"));

        _resolveRoleType = _roleTypeLookupServiceMock.Object.FindAsync;
    }

    [Fact]
    public void GivenNullableParties_WhenCallingToSilver_ShouldReturnEmptyList()
    {
        var results = CtsPartyRoleRelationshipMapper.ToSilver(null!);

        results.Should().NotBeNull();
        results.Count.Should().Be(0);
    }

    [Fact]
    public void GivenEmptyParties_WhenCallingToSilver_ShouldReturnEmptyList()
    {
        var results = CtsPartyRoleRelationshipMapper.ToSilver([]);

        results.Should().NotBeNull();
        results.Count.Should().Be(0);
    }

    [Theory]
    [InlineData(1, InferredRoleType.Agent)]
    [InlineData(2, InferredRoleType.Agent)]
    [InlineData(1, InferredRoleType.LivestockKeeper)]
    [InlineData(2, InferredRoleType.LivestockKeeper)]
    public async Task GivenPartiesExist_WhenCallingToSilver_ShouldReturnPopulatedList(int quantity, InferredRoleType inferredRoleType)
    {
        var holdingIdentifier = CphGenerator.GenerateCtsFormattedLidIdentifier("AH");

        var records = GenerateCtsAgentOrKeeper(quantity, holdingIdentifier);

        var silverParties = await CtsAgentOrKeeperMapper.ToSilver(
            DateTime.UtcNow,
            records,
            HoldingIdentifierType.CphNumber,
            inferredRoleType,
            _resolveRoleType,
            CancellationToken.None);

        var results = CtsPartyRoleRelationshipMapper.ToSilver(silverParties);

        foreach (var party in silverParties)
        {
            party.Should().NotBeNull();
            party.Roles.Should().NotBeNull();

            foreach (var role in party.Roles)
            {
                var mapped = results.Single(r => r.PartyId == party.PartyId && r.RoleTypeId == role.RoleTypeId);
                VerifyCtsPartyRoleRelationshipMappings.VerifyMapping_From_CtsPartyDocument_To_PartyRoleRelationshipDocument(
                    party,
                    mapped,
                    holdingIdentifier.LidIdentifierToCph(),
                    HoldingIdentifierType.CphNumber.ToString());
            }
        }
    }

    private static List<CtsAgentOrKeeper> GenerateCtsAgentOrKeeper(int quantity, string holdingIdentifier)
    {
        var records = new List<CtsAgentOrKeeper>();
        var factory = new MockCtsRawDataFactory();
        for (var i = 0; i < quantity; i++)
        {
            records.Add(factory.CreateMockAgentOrKeeper(
                changeType: DataBridgeConstants.ChangeTypeInsert,
                batchId: 1,
                holdingIdentifier: holdingIdentifier,
                endDate: DateTime.UtcNow.Date));
        }
        return records;
    }
}