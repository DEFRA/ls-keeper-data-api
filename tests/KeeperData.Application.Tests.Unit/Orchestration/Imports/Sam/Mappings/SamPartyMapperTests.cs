using FluentAssertions;
using KeeperData.Application.Orchestration.Imports.Sam.Mappings;
using KeeperData.Core.ApiClients.DataBridgeApi;
using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Core.Services;
using KeeperData.Tests.Common.Factories;
using KeeperData.Tests.Common.Generators;
using KeeperData.Tests.Common.Mappings;
using Moq;

namespace KeeperData.Application.Tests.Unit.Orchestration.Imports.Sam.Mappings;

public class SamPartyMapperTests
{
    private readonly Mock<IRoleTypeLookupService> _roleTypeLookupServiceMock = new();
    private readonly Mock<ICountryIdentifierLookupService> _countryIdentifierLookupServiceMock = new();

    private readonly Func<string?, CancellationToken, Task<(string?, string?)>> _resolveRoleType;
    private readonly Func<string?, CancellationToken, Task<(string?, string?)>> _resolveCountry;

    public SamPartyMapperTests()
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
    public async Task GivenNullableRawParties_WhenCallingToSilver_ShouldReturnEmptyList()
    {
        var results = await SamPartyMapper.ToSilver(
            (List<SamParty>?)null!,
            _resolveRoleType,
            _resolveCountry,
            CancellationToken.None);

        results.Should().NotBeNull();
        results.Count.Should().Be(0);
    }

    [Fact]
    public async Task GivenEmptyRawParties_WhenCallingToSilver_ShouldReturnEmptyList()
    {
        var results = await SamPartyMapper.ToSilver(
            [],
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

        var records = GenerateSamParty(1);

        var results = await SamPartyMapper.ToSilver(
            records,
            _resolveRoleType,
            _resolveCountry,
            CancellationToken.None);

        results.Should().NotBeNull();
        results.Count.Should().Be(1);

        var sourceRoleList = records[0].ROLES?.Split(",")
            .Where(role => !string.IsNullOrWhiteSpace(role))
            .Select(role => role.Trim())
            .ToArray() ?? [];

        var result = results[0];
        result.Roles.Should().NotBeNull().And.HaveCount(0);
    }

    [Fact]
    public async Task GivenFindCountryDoesNotMatch_WhenCallingToSilver_ShouldReturnEmptyCountryDetails()
    {
        _countryIdentifierLookupServiceMock
            .Setup(x => x.FindAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((null, null));

        var records = GenerateSamParty(1);

        var results = await SamPartyMapper.ToSilver(
            records,
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
    [InlineData(1)]
    [InlineData(2)]
    public async Task GivenRawParties_WhenCallingToSilver_ShouldReturnPopulatedList(int quantity)
    {
        var records = GenerateSamParty(quantity);

        var holdingIdentifier = CphGenerator.GenerateFormattedCph();

        var results = await SamPartyMapper.ToSilver(
            records,
            _resolveRoleType,
            _resolveCountry,
            CancellationToken.None);

        results.Should().NotBeNull();
        results.Count.Should().Be(quantity);

        for (var i = 0; i < quantity; i++)
        {
            VerifySamPartyMappings.VerifyMapping_From_SamParty_To_SamPartyDocument(records[i], results[i]);
        }
    }

    // TODO - Add tests for AggregatePartyAndHolder

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