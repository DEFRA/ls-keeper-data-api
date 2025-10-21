using FluentAssertions;
using KeeperData.Application.Orchestration.Cts.Inserts.Mappings;
using KeeperData.Core.ApiClients.DataBridgeApi;
using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Core.Domain.Enums;
using KeeperData.Core.Services;
using KeeperData.Tests.Common.Factories;
using KeeperData.Tests.Common.Generators;
using KeeperData.Tests.Common.Mappings;
using Moq;

namespace KeeperData.Application.Tests.Unit.Orchestration.Cts.Inserts;

public class CtsPartyRoleRelationshipMapperTests
{
    private readonly Mock<IRoleTypeLookupService> _roleTypeLookupServiceMock = new();
    private readonly Func<string, CancellationToken, Task<(string?, string?)>> _resolveRoleType;

    public CtsPartyRoleRelationshipMapperTests()
    {
        _roleTypeLookupServiceMock
            .Setup(x => x.FindRoleAsync("Agent", It.IsAny<CancellationToken>()))
            .ReturnsAsync(("AgentId", "Agent"));

        _roleTypeLookupServiceMock
            .Setup(x => x.FindRoleAsync("Keeper", It.IsAny<CancellationToken>()))
            .ReturnsAsync(("KeeperId", "Keeper"));

        _resolveRoleType = _roleTypeLookupServiceMock.Object.FindRoleAsync;
    }

    [Fact]
    public void GivenNullableParties_WhenCallingToSilver_ShouldReturnEmptyList()
    {
        var results = CtsPartyRoleRelationshipMapper.ToSilver(null!,
            Guid.NewGuid().ToString(),
            Guid.NewGuid().ToString());

        results.Should().NotBeNull();
        results.Count.Should().Be(0);
    }

    [Fact]
    public void GivenEmptyParties_WhenCallingToSilver_ShouldReturnEmptyList()
    {
        var results = CtsPartyRoleRelationshipMapper.ToSilver([],
            Guid.NewGuid().ToString(),
            Guid.NewGuid().ToString());

        results.Should().NotBeNull();
        results.Count.Should().Be(0);
    }

    [Theory]
    [InlineData(1, InferredRoleType.Agent)]
    [InlineData(2, InferredRoleType.Agent)]
    [InlineData(1, InferredRoleType.PrimaryKeeper)]
    [InlineData(2, InferredRoleType.PrimaryKeeper)]
    public async Task GivenPartiesExist_WhenCallingToSilver_ShouldReturnPopulatedList(int quantity, InferredRoleType inferredRoleType)
    {
        var records = GenerateCtsAgentOrKeeper(quantity);

        var silverParties = await CtsAgentOrKeeperMapper.ToSilver(records,
            inferredRoleType,
            _resolveRoleType,
            CancellationToken.None);

        var holdingIdentifier = Guid.NewGuid().ToString();
        var holdingIdentifierType = Guid.NewGuid().ToString();

        var results = CtsPartyRoleRelationshipMapper.ToSilver(silverParties,
            holdingIdentifier,
            holdingIdentifierType);

        foreach (var party in silverParties)
        {
            party.Should().NotBeNull();
            party.Roles.Should().NotBeNull();

            foreach (var role in party.Roles)
            {
                var mapped = results.Single(r => r.Id == role.IdentifierId);
                VerifyCtsPartyRoleRelationshipMappings.VerifyMapping_From_CtsPartyDocument_To_PartyRoleRelationshipDocument(
                    party,
                    mapped,
                    holdingIdentifier,
                    holdingIdentifierType);
            }
        }
    }

    private static List<CtsAgentOrKeeper> GenerateCtsAgentOrKeeper(int quantity)
    {
        var records = new List<CtsAgentOrKeeper>();
        var factory = new MockCtsDataFactory();
        for (var i = 0; i < quantity; i++)
        {
            records.Add(factory.CreateMockAgentOrKeeper(
                changeType: DataBridgeConstants.ChangeTypeInsert,
                batchId: 1,
                holdingIdentifier: CphGenerator.GenerateFormattedCph(),
                endDate: DateTime.UtcNow.Date));
        }
        return records;
    }
}