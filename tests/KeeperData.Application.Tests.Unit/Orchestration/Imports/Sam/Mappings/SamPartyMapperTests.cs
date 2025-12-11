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
            Guid.NewGuid().ToString(),
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
            Guid.NewGuid().ToString(),
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

        var holdingIdentifier = CphGenerator.GenerateFormattedCph();

        var records = GenerateSamParty(1);

        var results = await SamPartyMapper.ToSilver(
            holdingIdentifier,
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

        var holdingIdentifier = CphGenerator.GenerateFormattedCph();

        var records = GenerateSamParty(1);

        var results = await SamPartyMapper.ToSilver(
            holdingIdentifier,
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
            holdingIdentifier,
            records,
            _resolveRoleType,
            _resolveCountry,
            CancellationToken.None);

        results.Should().NotBeNull();
        results.Count.Should().Be(quantity);

        for (var i = 0; i < quantity; i++)
        {
            VerifySamPartyMappings.VerifyMapping_From_SamParty_To_SamPartyDocument(records[i], results[i], sourceIsHolder: false);
        }
    }

    [Fact]
    public async Task GivenRawPartyAndHolder_WhenCallingToSilver_ShouldReturnPopulatedList()
    {
        var quantity = 2;
        var records = GenerateSamParty(1);
        records.AddRange(GenerateSamCphHolderAsParty(1));

        var holdingIdentifier = CphGenerator.GenerateFormattedCph();

        var results = await SamPartyMapper.ToSilver(
            holdingIdentifier,
            records,
            _resolveRoleType,
            _resolveCountry,
            CancellationToken.None);

        results.Should().NotBeNull();
        results.Count.Should().Be(quantity);

        for (var i = 0; i < quantity; i++)
        {
            VerifySamPartyMappings.VerifyMapping_From_SamParty_To_SamPartyDocument(records[i], results[i], sourceIsHolder: i == 1);
        }
    }

    [Fact]
    public void GivenNoPartiesOrHolders_WhenAggregatingPartyAndHolder_ShouldReturnEmptyList()
    {
        var result = SamPartyMapper.AggregatePartyAndHolder([], []);

        result.Should().BeEmpty();
    }

    [Fact]
    public void GivenDifferingPartyAndHolder_WhenAggregatingPartyAndHolder_ShouldReturnNonMergedList()
    {
        var parties = GenerateSamParty(1);
        var holders = GenerateSamCphHolder(1);

        var result = SamPartyMapper.AggregatePartyAndHolder(parties, holders);

        result.Should().HaveCount(2);
        result[0].PARTY_ID.Should().NotBe(result[1].PARTY_ID);
        result[0].CPHS.Should().BeNull();
        result[1].CPHS.Should().NotBeNull();
        result[0].RoleList.Should().HaveCount(parties[0].RoleList.Count);
        result[1].RoleList.Should().HaveCount(1);
    }

    [Fact]
    public void GivenMatchingPartyAndHolder_WhenAggregatingPartyAndHolder_ShouldReturnMergedList()
    {
        var parties = GenerateSamParty(1);
        var holders = GenerateSamCphHolder(1);
        holders[0].PARTY_ID = parties[0].PARTY_ID;
        var totalRoles = parties[0].RoleList.Count;

        var result = SamPartyMapper.AggregatePartyAndHolder(parties, holders);

        result.Should().HaveCount(1);
        result[0].CPHS.Should().NotBeNull();
        result[0].RoleList.Should().HaveCount(totalRoles + 1);
    }

    [Fact]
    public void GivenMatchingPartyAndHolder_WhenAggregatingPartyAndHolder_ShouldReturnMergedList_AndMergeFields()
    {
        var parties = new List<SamParty>
        {
            new()
            {
                PARTY_ID = "P1",
                PERSON_TITLE = "Mr",
                PERSON_GIVEN_NAME = "John",
                PERSON_FAMILY_NAME = "Doe",
                TELEPHONE_NUMBER = "02012345678",
                INTERNET_EMAIL_ADDRESS = "john.doe1@email.co.uk",
                STREET = "Street 1",
                COUNTRY_CODE = "GB",
                ROLES = "Keeper"
            }
        };

        var holders = new List<SamCphHolder>
        {
            new()
            {
                PARTY_ID = "P1",
                PERSON_TITLE = "Mr",
                PERSON_GIVEN_NAME = "John",
                PERSON_FAMILY_NAME = "Doe",
                MOBILE_NUMBER = "07712345678",
                INTERNET_EMAIL_ADDRESS = "john.doe2@email.co.uk",
                STREET = "Street 2",
                TOWN = "Town",
                LOCALITY = "Locality",
                COUNTRY_CODE = "GB",
                CPHS = "11/234/5678"
            }
        };

        var totalRoles = parties[0].RoleList.Count;

        var result = SamPartyMapper.AggregatePartyAndHolder(parties, holders);

        result.Should().HaveCount(1);

        var party = result[0];
        party.CPHS.Should().NotBeNull();
        party.RoleList.Should().HaveCount(2);
        party.TELEPHONE_NUMBER.Should().Be("02012345678");
        party.INTERNET_EMAIL_ADDRESS.Should().Be("john.doe1@email.co.uk");
        party.MOBILE_NUMBER.Should().Be("07712345678");
        party.STREET.Should().Be("Street 1");
        party.TOWN.Should().Be("Town");
        party.LOCALITY.Should().Be("Locality");
        party.CPHS.Should().Be("11/234/5678");
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

    private static List<SamParty> GenerateSamCphHolderAsParty(int quantity)
    {
        var records = GenerateSamCphHolder(quantity);
        return SamPartyMapper.AggregatePartyAndHolder([], records);
    }
}