using FluentAssertions;
using KeeperData.Application.Extensions;
using KeeperData.Application.Orchestration.Sam.Holdings.Mappings;
using KeeperData.Core.ApiClients.DataBridgeApi;
using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Core.Domain.Enums;
using KeeperData.Core.Services;
using KeeperData.Tests.Common.Factories;
using KeeperData.Tests.Common.Generators;
using KeeperData.Tests.Common.Mappings;
using Moq;

namespace KeeperData.Application.Tests.Unit.Orchestration.Sam.Holdings.Mappings;

public class SamHolderMapperTests
{
    private readonly Mock<IRoleTypeLookupService> _roleTypeLookupServiceMock = new();
    private readonly Mock<ICountryIdentifierLookupService> _countryIdentifierLookupServiceMock = new();

    private readonly Func<string?, CancellationToken, Task<(string?, string?)>> _resolveRoleType;
    private readonly Func<string?, CancellationToken, Task<(string?, string?)>> _resolveCountry;

    public SamHolderMapperTests()
    {
        _roleTypeLookupServiceMock
            .Setup(x => x.FindAsync("Holder", It.IsAny<CancellationToken>()))
            .ReturnsAsync((string? input, CancellationToken token) => (Guid.NewGuid().ToString(), input));

        _countryIdentifierLookupServiceMock
            .Setup(x => x.FindAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string? input, CancellationToken token) => (Guid.NewGuid().ToString(), input));

        _resolveRoleType = _roleTypeLookupServiceMock.Object.FindAsync;
        _resolveCountry = _countryIdentifierLookupServiceMock.Object.FindAsync;
    }

    [Fact]
    public async Task GivenNullableRawHolders_WhenCallingToSilver_ShouldReturnEmptyList()
    {
        var results = await SamHolderMapper.ToSilver(null!,
            Guid.NewGuid().ToString(),
            InferredRoleType.Holder,
            _resolveRoleType,
            _resolveCountry,
            CancellationToken.None);

        results.Should().NotBeNull();
        results.Count.Should().Be(0);
    }

    [Fact]
    public async Task GivenEmptyRawHolders_WhenCallingToSilver_ShouldReturnEmptyList()
    {
        var results = await SamHolderMapper.ToSilver([],
            Guid.NewGuid().ToString(),
            InferredRoleType.Holder,
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

        var results = await SamHolderMapper.ToSilver(records,
            Guid.NewGuid().ToString(),
            InferredRoleType.Holder,
            _resolveRoleType,
            _resolveCountry,
            CancellationToken.None);

        results.Should().NotBeNull();
        results.Count.Should().Be(1);

        var result = results[0];
        result.Roles.Should().NotBeNull().And.HaveCount(1);

        var role = result.Roles[0];
        role.IdentifierId.Should().NotBeNullOrWhiteSpace();
        role.SourceRoleName.Should().Be(InferredRoleType.Holder.GetDescription());
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

        var results = await SamHolderMapper.ToSilver(records,
            Guid.NewGuid().ToString(),
            InferredRoleType.Holder,
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
    [InlineData(1, InferredRoleType.Holder)]
    [InlineData(2, InferredRoleType.Holder)]
    public async Task GivenRawHolders_WhenCallingToSilver_ShouldReturnPopulatedList(int quantity, InferredRoleType inferredRoleType)
    {
        var records = GenerateSamCphHolder(quantity);

        var holdingIdentifier = Guid.NewGuid().ToString();

        var results = await SamHolderMapper.ToSilver(records,
            holdingIdentifier,
            inferredRoleType,
            _resolveRoleType,
            _resolveCountry,
            CancellationToken.None);

        results.Should().NotBeNull();
        results.Count.Should().Be(quantity);

        for (var i = 0; i < quantity; i++)
        {
            VerifySamHolderMappings.VerifyMapping_From_SamCphHolder_To_SamPartyDocument(holdingIdentifier, records[i], results[i], inferredRoleType);
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