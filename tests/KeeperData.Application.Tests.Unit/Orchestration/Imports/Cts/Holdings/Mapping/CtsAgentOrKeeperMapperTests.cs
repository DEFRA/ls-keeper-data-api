using FluentAssertions;
using KeeperData.Application.Extensions;
using KeeperData.Application.Orchestration.Imports.Cts.Mappings;
using KeeperData.Core.ApiClients.DataBridgeApi;
using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Core.Domain.Enums;
using KeeperData.Core.Services;
using KeeperData.Tests.Common.Factories;
using KeeperData.Tests.Common.Generators;
using KeeperData.Tests.Common.Mappings;
using Moq;

namespace KeeperData.Application.Tests.Unit.Orchestration.Imports.Cts.Holdings.Mapping;

public class CtsAgentOrKeeperMapperTests
{
    private readonly Mock<IRoleTypeLookupService> _roleTypeLookupServiceMock = new();
    private readonly Func<string?, CancellationToken, Task<(string?, string?)>> _resolveRoleType;

    public CtsAgentOrKeeperMapperTests()
    {
        _roleTypeLookupServiceMock
            .Setup(x => x.FindAsync(EnumExtensions.GetDescription(InferredRoleType.Agent), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string? input, CancellationToken token) => (Guid.NewGuid().ToString(), InferredRoleType.Agent.ToString()));

        _roleTypeLookupServiceMock
            .Setup(x => x.FindAsync(EnumExtensions.GetDescription(InferredRoleType.LivestockKeeper), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string? input, CancellationToken token) => (Guid.NewGuid().ToString(), InferredRoleType.LivestockKeeper.ToString()));

        _resolveRoleType = _roleTypeLookupServiceMock.Object.FindAsync;
    }

    [Fact]
    public async Task GivenNullableRawAgentOrKeepers_WhenCallingToSilver_ShouldReturnEmptyList()
    {
        var results = await CtsAgentOrKeeperMapper.ToSilver(
            null!,
            HoldingIdentifierType.CphNumber,
            InferredRoleType.Agent,
            _resolveRoleType,
            CancellationToken.None);

        results.Should().NotBeNull();
        results.Count.Should().Be(0);
    }

    [Fact]
    public async Task GivenEmptyRawAgentOrKeepers_WhenCallingToSilver_ShouldReturnEmptyList()
    {
        var results = await CtsAgentOrKeeperMapper.ToSilver(
            [],
            HoldingIdentifierType.CphNumber,
            InferredRoleType.Agent,
            _resolveRoleType,
            CancellationToken.None);

        results.Should().NotBeNull();
        results.Count.Should().Be(0);
    }

    [Fact]
    public async Task GivenFindRoleDoesNotMatch_WhenCallingToSilver_ShouldReturnEmptyRoleDetails()
    {
        _roleTypeLookupServiceMock
            .Setup(x => x.FindAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((null, null));

        var records = GenerateCtsAgentOrKeeper(1);

        var results = await CtsAgentOrKeeperMapper.ToSilver(
            records,
            HoldingIdentifierType.CphNumber,
            InferredRoleType.Agent,
            _resolveRoleType,
            CancellationToken.None);

        results.Should().NotBeNull();
        results.Count.Should().Be(1);

        var result = results[0];
        result.Roles.Should().NotBeNull().And.HaveCount(1);

        var role = result.Roles[0];
        role.IdentifierId.Should().NotBeNullOrWhiteSpace();
        role.SourceRoleName.Should().Be(InferredRoleType.Agent.GetDescription());
        role.RoleTypeId.Should().BeNull();
        role.RoleTypeName.Should().BeNull();
        role.EffectiveFromDate.Should().Be(records[0].LPR_EFFECTIVE_FROM_DATE);
        role.EffectiveToDate.Should().Be(records[0].LPR_EFFECTIVE_TO_DATE);
    }

    [Theory]
    [InlineData(1, InferredRoleType.Agent)]
    [InlineData(2, InferredRoleType.Agent)]
    [InlineData(1, InferredRoleType.LivestockKeeper)]
    [InlineData(2, InferredRoleType.LivestockKeeper)]
    public async Task GivenRawAgentOrKeepers_WhenCallingToSilver_ShouldReturnPopulatedList(int quantity, InferredRoleType inferredRoleType)
    {
        var records = GenerateCtsAgentOrKeeper(quantity);

        var results = await CtsAgentOrKeeperMapper.ToSilver(
            records,
            HoldingIdentifierType.CphNumber,
            inferredRoleType,
            _resolveRoleType,
            CancellationToken.None);

        results.Should().NotBeNull();
        results.Count.Should().Be(quantity);

        for (var i = 0; i < quantity; i++)
        {
            VerifyCtsAgentOrKeeperMappings.VerifyMapping_From_CtsAgentOrKeeper_To_CtsPartyDocument(records[i], results[i], inferredRoleType);
        }
    }

    private static List<CtsAgentOrKeeper> GenerateCtsAgentOrKeeper(int quantity)
    {
        var records = new List<CtsAgentOrKeeper>();
        var factory = new MockCtsRawDataFactory();
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