using FluentAssertions;
using KeeperData.Application.Extensions;
using KeeperData.Application.Orchestration.Sam.Mappings;
using KeeperData.Core.ApiClients.DataBridgeApi;
using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Core.Domain.Enums;
using KeeperData.Core.Services;
using KeeperData.Tests.Common.Factories;
using KeeperData.Tests.Common.Generators;
using KeeperData.Tests.Common.Mappings;
using Moq;

namespace KeeperData.Application.Tests.Unit.Orchestration.Sam.Mappings;

public class SamHolderMapperTests
{
    private readonly Mock<IRoleTypeLookupService> _roleTypeLookupServiceMock = new();
    private readonly Mock<ICountryIdentifierLookupService> _countryIdentifierLookupServiceMock = new();

    private readonly Func<string?, CancellationToken, Task<(string?, string?)>> _resolveRoleType;
    private readonly Func<string?, CancellationToken, Task<(string?, string?)>> _resolveCountry;

    public SamHolderMapperTests()
    {
        _roleTypeLookupServiceMock
            .Setup(x => x.FindAsync(InferredRoleType.CphHolder.GetDescription(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string? input, CancellationToken token) => (Guid.NewGuid().ToString(), InferredRoleType.CphHolder.ToString()));

        _countryIdentifierLookupServiceMock
            .Setup(x => x.FindAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string? input, CancellationToken token) => (Guid.NewGuid().ToString(), input));

        _resolveRoleType = _roleTypeLookupServiceMock.Object.FindAsync;
        _resolveCountry = _countryIdentifierLookupServiceMock.Object.FindAsync;
    }

    [Fact]
    public async Task GivenNullableRawHolders_WhenCallingToSilver_ShouldReturnEmptyList()
    {
        var results = await SamHolderMapper.ToSilver(
            DateTime.UtcNow,
            null!,
            InferredRoleType.CphHolder,
            _resolveRoleType,
            _resolveCountry,
            CancellationToken.None);

        results.Should().NotBeNull();
        results.Count.Should().Be(0);
    }

    [Fact]
    public async Task GivenEmptyRawHolders_WhenCallingToSilver_ShouldReturnEmptyList()
    {
        var results = await SamHolderMapper.ToSilver(
            DateTime.UtcNow,
            [],
            InferredRoleType.CphHolder,
            _resolveRoleType,
            _resolveCountry,
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

        var records = GenerateSamCphHolder(1);

        var results = await SamHolderMapper.ToSilver(
            DateTime.UtcNow,
            records,
            InferredRoleType.CphHolder,
            _resolveRoleType,
            _resolveCountry,
            CancellationToken.None);

        results.Should().NotBeNull();
        results.Count.Should().Be(1);

        var result = results[0];
        result.Roles.Should().NotBeNull().And.HaveCount(1);

        var role = result.Roles[0];
        role.IdentifierId.Should().NotBeNullOrWhiteSpace();
        role.SourceRoleName.Should().Be(InferredRoleType.CphHolder.GetDescription());
        role.RoleTypeId.Should().BeNull();
        role.RoleTypeName.Should().BeNull();
    }

    [Fact]
    public async Task GivenFindCountryDoesNotMatch_WhenCallingToSilver_ShouldReturnEmptyCountryDetails()
    {
        _countryIdentifierLookupServiceMock
            .Setup(x => x.FindAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((null, null));

        var records = GenerateSamCphHolder(1);

        var results = await SamHolderMapper.ToSilver(
            DateTime.UtcNow,
            records,
            InferredRoleType.CphHolder,
            _resolveRoleType,
            _resolveCountry,
            CancellationToken.None);

        results.Should().NotBeNull();
        results.Count.Should().Be(1);

        var result = results[0];
        result.Address.Should().NotBeNull();

        var address = result.Address;
        address.IdentifierId.Should().NotBeNullOrWhiteSpace();
        address.CountryCode.Should().Be(records[0].COUNTRY_CODE);
        address.CountryIdentifier.Should().BeNull();
    }

    [Theory]
    [InlineData(1, InferredRoleType.CphHolder)]
    [InlineData(2, InferredRoleType.CphHolder)]
    public async Task GivenRawHolders_WhenCallingToSilver_ShouldReturnPopulatedList(int quantity, InferredRoleType inferredRoleType)
    {
        var records = GenerateSamCphHolder(quantity);

        var holdingIdentifier = CphGenerator.GenerateFormattedCph();

        var results = await SamHolderMapper.ToSilver(
            DateTime.UtcNow,
            records,
            inferredRoleType,
            _resolveRoleType,
            _resolveCountry,
            CancellationToken.None);

        results.Should().NotBeNull();
        results.Count.Should().Be(quantity);

        for (var i = 0; i < quantity; i++)
        {
            VerifySamHolderMappings.VerifyMapping_From_SamCphHolder_To_SamPartyDocument(records[i], results[i], inferredRoleType);
        }
    }

    private static List<SamCphHolder> GenerateSamCphHolder(int quantity)
    {
        var records = new List<SamCphHolder>();
        var factory = new MockSamRawDataFactory();
        for (var i = 0; i < quantity; i++)
        {
            records.Add(factory.CreateMockHolder(
                changeType: DataBridgeConstants.ChangeTypeInsert,
                batchId: 1,
                holdingIdentifiers: [CphGenerator.GenerateFormattedCph()]));
        }
        return records;
    }
}