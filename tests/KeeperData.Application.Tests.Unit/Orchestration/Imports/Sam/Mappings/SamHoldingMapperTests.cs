using FluentAssertions;
using KeeperData.Application.Orchestration.Imports.Sam.Mappings;
using KeeperData.Core.ApiClients.DataBridgeApi;
using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Core.Services;
using KeeperData.Tests.Common.Factories;
using KeeperData.Tests.Common.Generators;
using KeeperData.Tests.Common.Mappings;
using KeeperData.Tests.Common.TestData.Sam;
using Moq;

namespace KeeperData.Application.Tests.Unit.Orchestration.Imports.Sam.Mappings;

public class SamHoldingMapperTests
{
    private readonly Mock<ISiteActivityTypeLookupService> _siteActivityTypeLookupServiceMock = new();
    private readonly Mock<ISiteTypeLookupService> _siteTypeLookupServiceMock = new();
    private readonly Mock<ICountryIdentifierLookupService> _countryIdentifierLookupServiceMock = new();
    private readonly Mock<IActivityCodeLookupService> _activityCodeLookupServiceMock = new();
    private readonly Func<string?, CancellationToken, Task<(string?, string?)>> _resolveSiteActivityType;
    private readonly Func<string?, CancellationToken, Task<(string?, string?)>> _resolveSiteType;
    private readonly Func<string?, string?, CancellationToken, Task<(string?, string?, string?)>> _resolveCountry;

    public SamHoldingMapperTests()
    {
        _siteActivityTypeLookupServiceMock
            .Setup(x => x.FindAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string? input, CancellationToken token) => (Guid.NewGuid().ToString(), input));

        _siteTypeLookupServiceMock
            .Setup(x => x.FindAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string? input, CancellationToken token) => (Guid.NewGuid().ToString(), input));

        _countryIdentifierLookupServiceMock
            .Setup(x => x.FindAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string? input, string? internalCode, CancellationToken token) => (Guid.NewGuid().ToString(), input, input));

        _activityCodeLookupServiceMock.Setup(x => x.FindByActivityCodeAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string? key, CancellationToken token) => SamTestScenarios.LookupCodes(key));

        _resolveSiteActivityType = _siteActivityTypeLookupServiceMock.Object.FindAsync;
        _resolveSiteType = _siteTypeLookupServiceMock.Object.FindAsync;
        _resolveCountry = _countryIdentifierLookupServiceMock.Object.FindAsync;
    }

    [Fact]
    public async Task GivenNullableRawHoldings_WhenCallingToSilver_ShouldReturnEmptyList()
    {
        var results = await SamHoldingMapper.ToSilver(
            (List<SamCphHolding>?)null!,
            _resolveSiteActivityType,
            _resolveSiteType,
            _resolveCountry,
            CancellationToken.None);

        results.Should().NotBeNull();
        results.Count.Should().Be(0);
    }

    [Fact]
    public async Task GivenEmptyRawHoldings_WhenCallingToSilver_ShouldReturnEmptyList()
    {
        var results = await SamHoldingMapper.ToSilver(
            [],
            _resolveSiteActivityType,
            _resolveSiteType,
            _resolveCountry,
            CancellationToken.None);

        results.Should().NotBeNull();
        results.Count.Should().Be(0);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public async Task GivenRawHoldings_WhenCallingToSilver_ShouldReturnPopulatedList(int quantity)
    {
        var records = GenerateSamCphHolding(quantity);

        var results = await SamHoldingMapper.ToSilver(
            records,
            _resolveSiteActivityType,
            _resolveSiteType,
            _resolveCountry,
            CancellationToken.None);

        results.Should().NotBeNull();
        results.Count.Should().Be(quantity);

        for (var i = 0; i < quantity; i++)
        {
            VerifySamHoldingMappings.VerifyMapping_From_SamCphHolding_To_SamHoldingDocument(records[i], results[i]);
        }
    }

    // TODO - ADD ToGold Tests

    private static List<SamCphHolding> GenerateSamCphHolding(int quantity)
    {
        var records = new List<SamCphHolding>();
        var factory = new MockSamRawDataFactory();
        for (var i = 0; i < quantity; i++)
        {
            records.Add(factory.CreateMockHolding(
                changeType: DataBridgeConstants.ChangeTypeInsert,
                batchId: 1,
                holdingIdentifier: CphGenerator.GenerateFormattedCph(),
                endDate: DateTime.UtcNow.Date));
        }
        return records;
    }
}